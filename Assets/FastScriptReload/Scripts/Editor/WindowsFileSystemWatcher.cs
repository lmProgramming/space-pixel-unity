#if UNITY_2021_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using UnityEditor;

namespace FastScriptReload.Editor
{
    /// <summary>
    ///     This is a Windows only file watcher, for use in Unity/Mono.
    ///     Mono already has the cross platform FileSystemWatcher.
    ///     However, it's incredibly slow on Windows.
    ///     This one is fast.
    ///     This doesn't include the complete API surface of FileSystemWatcher,
    ///     but those bits that are present should be compatible with FileSystemWatcher.
    ///     Events will be dispatched on a worker thread.
    ///     They may be on different threads from each other, but they won't overlap in time.
    ///     There is an issue where there's a (small) chance that events may be missed.
    ///     Firstly, this can happen if the event occurs in the brief time when previous events
    ///     are being recorded, before listening can start again.
    ///     This is not unique to this implementation - the Microsoft version has the same problem.
    ///     The issue should actually be a little less bad here. Microsoft fires its events on the
    ///     monitoring thread, and relies on the user to be smart about offloading them.
    ///     We fire our events on a dedicated event thread. Long running handlers shouldn't
    ///     pose a problem.
    ///     Secondly, this can also happen if the internal buffer used by the Windows API overflows.
    ///     It's set to the maximum size (which is larger than the Microsoft implementation default)
    ///     but it could theoretically happen.
    ///     The solution to both of these things, if we want to be completely robust, is to combine
    ///     the file watcher with polling to catch rare missed events.
    /// </summary>
    internal sealed class WindowsFileSystemWatcher : IDisposable
    {
        private readonly WeakDisposer _weakDisposer;
        private InterruptibleHandle _currentHandle;
        private bool _disposed;
        private Task _eventsTask;
        private Task _monitorTask;

        private string _path;


        public WindowsFileSystemWatcher()
        {
            _eventsTask = Task.CompletedTask;
            _weakDisposer = new WeakDisposer(this);

            AppDomain.CurrentDomain.DomainUnload += _weakDisposer.Dispose;

#if UNITY_EDITOR
            EditorApplication.quitting += _weakDisposer.Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += _weakDisposer.Dispose;
#endif
        }

        public NotifyFilters NotifyFilter { get; set; } =
            NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        public string Filter { get; set; } = "*.*";
        public bool IncludeSubdirectories { get; set; } = false;

        public string Path
        {
            get => _path;
            set
            {
                var changed = value != _path;
                _path = value;

                // Restart if the path changes.
                if (changed && EnableRaisingEvents)
                {
                    EnableRaisingEvents = false;
                    EnableRaisingEvents = true;
                }
            }
        }


        public bool EnableRaisingEvents
        {
            get => _currentHandle != null;
            set
            {
                if (value)
                {
                    if (_disposed) throw new ObjectDisposedException(nameof(WindowsFileSystemWatcher));
                    if (_currentHandle != null) return;
                    _currentHandle = CreateDirectoryHandle(Path);
                    _monitorTask =
                        Task.Factory.StartNew(() => Monitor(_currentHandle), TaskCreationOptions.LongRunning);
                }
                else
                {
                    // This cancels scheduled-but-unrun events, because they don't run if the handle is closed.
                    _currentHandle?.Dispose();
                    _currentHandle = null;
                    _monitorTask?.Wait();
                    _monitorTask = null;
                    // We don't wait for the events task, because we might be within the events task.
                    // (Ooh, the events are coming from WITHIN THE TASK. Scary.)
                    // This does leave a tiny chance that a single event could be triggered immediately after this,
                    // if we're not in the events task...
                    // TODO: Maybe fix this
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);

            EnableRaisingEvents = false;
            Changed = null;
            Created = null;
            Deleted = null;
            Renamed = null;
            Error = null;

            AppDomain.CurrentDomain.DomainUnload -= _weakDisposer.Dispose;

#if UNITY_EDITOR
            EditorApplication.quitting -= _weakDisposer.Dispose;
            AssemblyReloadEvents.beforeAssemblyReload -= _weakDisposer.Dispose;
#endif
        }

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event RenamedEventHandler Renamed;
        public event ErrorEventHandler Error;

        // Note that the GC shouldn't ever destroy the FSW while it's running,
        // even if no references to it are retained by the user.
        // The monitoring thread holds a reference.
        ~WindowsFileSystemWatcher()
        {
            Dispose();
        }

        private static InterruptibleHandle CreateDirectoryHandle(string directory)
        {
            const int fileListDirectory = 0x0001;
            const int fileShareRead = 0x00000001;
            const int fileShareWrite = 0x00000002;
            const int fileShareDelete = 0x00000004;
            const int openExisting = 3;
            const int fileFlagBackupSemantics = 0x02000000;

            // There might be a way to do this without the OS call?
            var directoryHandle = CreateFile(
                directory,
                fileListDirectory,
                fileShareRead | fileShareDelete | fileShareWrite,
                null,
                openExisting,
                fileFlagBackupSemantics,
                new SafeFileHandle(IntPtr.Zero, false)
            );

            if (directoryHandle == null || directoryHandle.IsInvalid)
                throw new IOException("Failed to obtain handle for directory.");

            return new InterruptibleHandle(directoryHandle);
        }

        private unsafe void Monitor(InterruptibleHandle handle)
        {
            // We try to minimise the processing time taken by the monitoring thread.
            // The longer it takes, the more likely a buffer overflow.
            // (At our 64*1028 buffer size we're probably 99.9% fine anyway. Famous last words...)
            // We do this by pushing processing immediately to another thread.
            // We swap out the buffers instead of spending any time reading them.
            // This is probably a silly micro optimisation, but it feels like "the right way".

            const int maxBufferPoolSize = 32; // Huge
            var bufferPool = new Stack<byte[]>(maxBufferPoolSize);

            while (handle.IsOpen)
            {
                byte[] buffer;
                lock (bufferPool)
                {
                    if (!bufferPool.TryPop(out buffer)) buffer = new byte[64 * 1024];
                }

                fixed (byte* bufferPointer = buffer)
                {
                    var ok = false;
                    var size = 0;

                    try
                    {
                        ok = ReadDirectoryChangesW(
                            handle,
                            new HandleRef(buffer, (IntPtr)bufferPointer),
                            buffer.Length,
                            IncludeSubdirectories ? 1 : 0,
                            (int)NotifyFilter,
                            out size,
                            null,
                            new HandleRef(null, IntPtr.Zero)
                        );
                    }
                    // The directory handle could be disposed from another thread.
                    // That's fine, we'll just end.
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (ArgumentNullException)
                    {
                    }
                    catch (Exception ex)
                    {
                        DispatchError(ex);
                    }

                    if (!handle.IsOpen)
                        break;

                    if (!ok)
                        DispatchError(new Win32Exception());

                    if (size == 0)
                        DispatchError(
                            new InternalBufferOverflowException($"Too many changes at once in directory: {Path}."));
                }

                // Let's prevent event dispatches from overlapping or being out of order,
                // because this is closer to FileSystemWatcher's behaviour.
                // Overlapping/OOO would be a pretty easy way to get a nasty bug in user code.
                _eventsTask = _eventsTask.ContinueWith(_ =>
                {
                    ProcessBufferOnEventThread(handle, buffer);

                    // Return to pool, preventing strange leak scenarios where the pool grows unreasonably large.
                    // That could happen if the event threads run for a very long time.
                    lock (bufferPool)
                    {
                        if (bufferPool.Count < maxBufferPoolSize) bufferPool.Push(buffer);
                    }
                });
            }

            handle.Dispose();


            void DispatchError(Exception ex)
            {
                _eventsTask = _eventsTask.ContinueWith(_ => Error?.Invoke(this, new ErrorEventArgs(ex)));
            }
        }

        private void ProcessBufferOnEventThread(InterruptibleHandle handle, ReadOnlySpan<byte> buffer)
        {
            ReadOnlySpan<char> oldName = default;
            var oldMatch = false;

            while (true)
            {
                if (handle == null || !handle.IsOpen) break;

                // We're dealing with file names as Spans for two reasons:
                //  - FileSystemName wants them that way.
                //  - We can avoid the string allocation for files that don't match.

                var nextEntryOffset = MemoryMarshal.Read<int>(buffer);
                var action = MemoryMarshal.Read<Action>(buffer.Slice(4));
                var nameLength = MemoryMarshal.Read<int>(buffer.Slice(8));
                var name = MemoryMarshal.Cast<byte, char>(buffer.Slice(12, nameLength));
                buffer = buffer.Slice(nextEntryOffset);

                var match = string.IsNullOrEmpty(Filter) || FileSystemName.MatchesSimpleExpression(Filter, name);

                try
                {
                    switch (action)
                    {
                        case Action.RenamedOld:
                            oldName = name;
                            oldMatch = match;
                            break;

                        case Action.RenamedNew:
                            if (match | oldMatch)
                                Renamed?.Invoke(this,
                                    new RenamedEventArgs(WatcherChangeTypes.Renamed, Path, name.ToString(),
                                        oldName.ToString()));
                            break;

                        default:
                            if (!match) break;
                            var nameStr = name.ToString();

                            switch (action)
                            {
                                case Action.Added:
                                    Created?.Invoke(this,
                                        new FileSystemEventArgs(WatcherChangeTypes.Created, Path, nameStr));
                                    break;
                                case Action.Modified:
                                    Changed?.Invoke(this,
                                        new FileSystemEventArgs(WatcherChangeTypes.Changed, Path, nameStr));
                                    break;
                                case Action.Removed:
                                    Deleted?.Invoke(this,
                                        new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path, nameStr));
                                    break;
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, new ErrorEventArgs(new Exception("Exception in event handler.", ex)));
                }

                if (nextEntryOffset == 0) break;
            }
        }


        private class InterruptibleHandle : IDisposable
        {
            private bool _closed;

            public InterruptibleHandle(SafeFileHandle handle)
            {
                Handle = handle;
            }

            public SafeFileHandle Handle { get; }
            public bool IsOpen => !_closed & !Handle.IsInvalid & !Handle.IsClosed;

            public unsafe void Dispose()
            {
                _closed = true;
                if (!Handle.IsClosed) CancelIoEx(Handle, null);
                Handle.Dispose();
                GC.SuppressFinalize(this);
            }

            ~InterruptibleHandle()
            {
                Dispose();
            }

            public static implicit operator SafeFileHandle(InterruptibleHandle handle)
            {
                return handle.Handle;
            }
        }

        /// <summary>
        ///     Dispose handles are setup via weak references.
        ///     We don't want the domain reload stuff to keep the FSW alive.
        ///     Note that the FSW shouldn't be collected whilst running anyway.
        /// </summary>
        private sealed class WeakDisposer
        {
            private readonly WeakReference<WindowsFileSystemWatcher> _fsw;

            public WeakDisposer(WindowsFileSystemWatcher fsw)
            {
                _fsw = new WeakReference<WindowsFileSystemWatcher>(fsw);
            }

            public void Dispose()
            {
                if (_fsw.TryGetTarget(out var fsw)) fsw.Dispose();
            }

            public void Dispose(object _, EventArgs __)
            {
                Dispose();
            }
        }


        #region Windows API

        private enum Action
        {
            Added = 1,
            Removed = 2,
            Modified = 3,
            RenamedOld = 4,
            RenamedNew = 5
        }

        [DllImport("__Internal", CharSet = CharSet.Auto, BestFitMapping = false)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            int dwShareMode,
            SecurityAttributes lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            SafeFileHandle hTemplateFile
        );

        [DllImport("__Internal", EntryPoint = "ReadDirectoryChangesW", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern unsafe bool ReadDirectoryChangesW(
            SafeFileHandle hDirectory,
            HandleRef lpBuffer,
            int nBufferLength,
            int bWatchSubtree,
            int dwNotifyFilter,
            out int lpBytesReturned,
            NativeOverlapped* overlappedPointer,
            HandleRef lpCompletionRoutine
        );

        [DllImport("__Internal")]
        private static extern unsafe bool CancelIoEx(SafeHandle handle, NativeOverlapped* lpOverlapped);

        private class SecurityAttributes
        {
        }

        #endregion
    }
}
#endif