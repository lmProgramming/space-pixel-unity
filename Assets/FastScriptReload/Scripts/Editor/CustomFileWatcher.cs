using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FastScriptReload.Editor;
using UnityEditor;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public class CustomFileWatcher : EditorWindow
{
    private static readonly Dictionary<string, HashEntry> _fileHashes;
    private static readonly object _stateLock = new();

    private static readonly object _listLock; // Shared lock object
    private static Thread _livewatcherThread;

    public static bool InitSignaled = false;
    private static readonly int WatcherThreadRunEveryNSeconds = 500; //TODO: expose in settings

    static CustomFileWatcher()
    {
        _fileHashes = new Dictionary<string, HashEntry>();
        _listLock = new object();
        _livewatcherThread = null;
    }

    private static void UpdateFileWatcher()
    {
        if (_fileHashes.Count > 0)
            foreach (var kvp in _fileHashes)
                CheckForChanges(kvp.Key, kvp.Value.SearchPattern, kvp.Value.IncludeSubdirectories);
        else
            Debug.LogError("File watcher has not been initialized yet. Please initialize first.");
    }

    public static void TryEnableLivewatching()
    {
        if (_livewatcherThread != null)
        {
            Debug.LogWarning("Livewatcher is already running.");
            return;
        }

        // Run on a separate thread every 1 second
        _livewatcherThread = new Thread(() =>
        {
            var timer = new Timer(state =>
            {
                // Go at it if we've initialized
                if (_fileHashes.Count > 0)
                    UpdateFileWatcher();
            }, null, 0, WatcherThreadRunEveryNSeconds);
        });

        _livewatcherThread.Start();
    }

    public static void InitializeSingularFilewatcher(string directoryPath, string searchPattern,
        bool includeSubdirectories)
    {
#if ImmersiveVrTools_DebugEnabled
        Debug.Log("Initializing hashes for directory: " + directoryPath);
#endif

        var thread = new Thread(() =>
        {
            lock (_stateLock)
            {
                var hashes = new Dictionary<string, string>();
                var files = Directory.GetFiles(directoryPath, searchPattern,
                    includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                foreach (var filePath in files)
                {
                    var hash = GetFileHash(filePath);
                    hashes[filePath] = hash;
                }

                _fileHashes[directoryPath] = new HashEntry(hashes, searchPattern, includeSubdirectories);
            }
        });
        thread.Start();
    }

    private static void CheckForChanges(string directoryPath, string searchPattern, bool includeSubdirectories)
    {
        // Not really sure if this nuclear locking is needed
        lock (_stateLock)
        {
            var hashes = _fileHashes[directoryPath].Hashes;

            // Time profiling: Start the stopwatch for Directory.GetFiles
#if ImmersiveVrTools_DebugEnabled
            System.Diagnostics.Stopwatch getFilesStopwatch = new System.Diagnostics.Stopwatch();
            getFilesStopwatch.Start();
#endif

            var files = Directory.GetFiles(directoryPath, searchPattern,
                includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

#if ImmersiveVrTools_DebugEnabled
            // Time profiling: Stop the stopwatch for Directory.GetFiles and log the elapsed time
            getFilesStopwatch.Stop();
            Debug.Log("Directory.GetFiles elapsed time: " + getFilesStopwatch.ElapsedMilliseconds + " ms");
#endif

            // Check if files were created or modified
            // Time profiling: Start the stopwatch for file creation/modification
            var fileChangeStopwatch = new Stopwatch();
            fileChangeStopwatch.Start();

            foreach (var file in files)
                if (!hashes.ContainsKey(file))
                {
                    // New file
#if ImmersiveVrTools_DebugEnabled
                    Debug.Log("New file: " + file);
#endif
                }

                else if (hashes[file] != GetFileHash(file))
                {
                    // File changed
#if ImmersiveVrTools_DebugEnabled
                    Debug.Log("File changed: " + file);
#endif
                    RecordChange(file);
                }

#if ImmersiveVrTools_DebugEnabled
            // Time profiling: Stop the stopwatch for file creation/modification and log the elapsed time
            fileChangeStopwatch.Stop();
            Debug.Log("File creation/modification elapsed time: " + fileChangeStopwatch.ElapsedMilliseconds + " ms");
#endif

            // Check if any files were deleted
            // Time profiling: Start the stopwatch for file deletion
            var fileDeletionStopwatch = new Stopwatch();
            fileDeletionStopwatch.Start();

            foreach (var kvp in hashes)
                if (!File.Exists(kvp.Key))
                {
#if ImmersiveVrTools_DebugEnabled
                    Debug.Log("File deleted: " + kvp.Key);
#endif
                }

            // Time profiling: Stop the stopwatch for file deletion and log the elapsed time
#if ImmersiveVrTools_DebugEnabled
            fileDeletionStopwatch.Stop();
            Debug.Log("File deletion elapsed time: " + fileDeletionStopwatch.ElapsedMilliseconds + " ms");
#endif

            // Update hashes
            hashes.Clear();
            foreach (var file in files)
            {
                var hash = GetFileHash(file);
                hashes[file] = hash;
            }
        }
    }


    private static string GetFileHash(string filePath)
    {
        using (var md5 = MD5.Create())
        using (var stream = File.OpenRead(filePath))
        {
            var hashBytes = md5.ComputeHash(stream);
            var sb = new StringBuilder();
            for (var i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));
            return sb.ToString();
        }
    }

    private static void RecordChange(string path)
    {
        if (FastScriptReloadManager.Instance.ShouldIgnoreFileChange()) return;

        lock (_listLock)
        {
            FastScriptReloadManager.Instance.AddFileChangeToProcess(path);
        }
    }

    public class HashEntry
    {
        // Some metadata for the update function to use
        // WARN: Note this data isn't exactly synced up or anything. It just reads it in when the filewatcher is initialized.

        public HashEntry(Dictionary<string, string> hashes, string searchPattern, bool includeSubdirectories)
        {
            Hashes = hashes;
            SearchPattern = searchPattern;
            IncludeSubdirectories = includeSubdirectories;
        }

        public Dictionary<string, string> Hashes { get; } = new();

        public string SearchPattern { get; }

        public bool IncludeSubdirectories { get; }
    }
}