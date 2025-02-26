using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastScriptReload.Editor.Compilation;
using FastScriptReload.Editor.Compilation.ScriptGenerationOverrides;
using FastScriptReload.Runtime;
using ImmersiveVRTools.Runtime.Common;
using ImmersiveVrToolsCommon.Runtime.Logging;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FastScriptReload.Editor
{
    [InitializeOnLoad]
    [PreventHotReload]
    public class FastScriptReloadManager
    {
        public const string FileWatcherReplacementTokenForApplicationDataPath = "<Application.dataPath>";

        private const int BaseMenuItemPriorityManualScriptOverride = 100;

        private const int BaseMenuItemPriorityExclusions = 200;
        private static FastScriptReloadManager _instance;

        private static readonly string _dataPath = Application.dataPath;

        private static bool _hotReloadDisabledWarningMessageShownAlready;
        private bool _assemblyChangesLoaderResolverResolutionAlreadyCalled;
        private IEnumerable<string> _currentFileExclusions;

        private readonly List<DynamicFileHotReloadState> _dynamicFileHotReloadStateEntries = new();
        private readonly List<FileSystemWatcher> _fileWatchers = new();
        private int _hotReloadPerformedCount;
        private bool _isEditorModeHotReloadEnabled;
        private bool _isOnDemandHotReloadEnabled;
        private PlayModeStateChange _lastPlayModeStateChange;

        private DateTime _lastTimeChangeBatchRun = default;
        private int _triggerDomainReloadIfOverNDynamicallyLoadedAssembles = 100;

        private bool _wasLockReloadAssembliesCalled;

        public Dictionary<string, Func<string>> FileWatcherTokensToResolvePathFn = new()
        {
            [FileWatcherReplacementTokenForApplicationDataPath] = () => _dataPath
        };

        static FastScriptReloadManager()
        {
            //do not add init code in here as with domain reload turned off it won't be properly set on play-mode enter, use Init method instead
            EditorApplication.update += Instance.Update;
            EditorApplication.playModeStateChanged += Instance.OnEditorApplicationOnplayModeStateChanged;
        }

        public static FastScriptReloadManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FastScriptReloadManager();
                    LoggerScoped.LogDebug("Created Manager");
                }

                return _instance;
            }
        }

        public bool EnableExperimentalThisCallLimitationFix { get; private set; }
#pragma warning disable 0618
        public AssemblyChangesLoaderEditorOptionsNeededInBuild
            AssemblyChangesLoaderEditorOptionsNeededInBuild { get; } = new();
#pragma warning restore 0618

        private void OnWatchedFileChange(object source, FileSystemEventArgs e)
        {
            if (!_isEditorModeHotReloadEnabled && _lastPlayModeStateChange != PlayModeStateChange.EnteredPlayMode)
            {
#if ImmersiveVrTools_DebugEnabled
            LoggerScoped.Log($"Application not playing, change to: {e.Name} won't be compiled and hot reloaded");
#endif
                return;
            }

            var filePathToUse = e.FullPath;
            if (!File.Exists(filePathToUse))
            {
                LoggerScoped.LogWarning(@"Fast Script Reload - Unity File Path Bug - Warning!
Path for changed file passed by Unity does not exist. This is a known editor bug, more info: https://issuetracker.unity3d.com/issues/filesystemwatcher-returns-bad-file-path
                    
Best course of action is to update editor as issue is already fixed in newer (minor and major) versions.
                    
As a workaround asset will try to resolve paths via directory search.
                    
Workaround will search in all folders (under project root) and will use first found file. This means it's possible it'll pick up wrong file as there's no directory information available.");

                var changedFileName = new FileInfo(filePathToUse).Name;
                //TODO: try to look in all file watcher configured paths, some users might have code outside of assets, eg packages
                // var fileFoundInAssets = FastScriptReloadPreference.FileWatcherSetupEntries.GetElementsTyped().SelectMany(setupEntries => Directory.GetFiles(DataPath, setupEntries.path, SearchOption.AllDirectories)).ToList();

                var fileFoundInAssets = Directory.GetFiles(_dataPath, changedFileName, SearchOption.AllDirectories);
                if (fileFoundInAssets.Length == 0)
                {
                    LoggerScoped.LogError(
                        $"FileWatcherBugWorkaround: Unable to find file '{changedFileName}', changes will not be reloaded. Please update unity editor.");
                    return;
                }

                if (fileFoundInAssets.Length == 1)
                {
                    LoggerScoped.Log(
                        $"FileWatcherBugWorkaround: Original Unity passed file path: '{e.FullPath}' adjusted to found: '{fileFoundInAssets[0]}'");
                    filePathToUse = fileFoundInAssets[0];
                }
                else
                {
                    LoggerScoped.LogWarning(
                        $"FileWatcherBugWorkaround: Multiple files found. Original Unity passed file path: '{e.FullPath}' adjusted to found: '{fileFoundInAssets[0]}'");
                    filePathToUse = fileFoundInAssets[0];
                }
            }

            if (_currentFileExclusions != null &&
                _currentFileExclusions.Any(fp => filePathToUse.Replace("\\", "/").EndsWith(fp)))
            {
                LoggerScoped.LogWarning(
                    $"FastScriptReload: File: '{filePathToUse}' changed, but marked as exclusion. Hot-Reload will not be performed. You can manage exclusions via" +
                    "\r\nRight click context menu (Fast Script Reload > Add / Remove Hot-Reload exclusion)" +
                    "\r\nor via Window -> Fast Script Reload -> Start Screen -> Exclusion menu");

                return;
            }

            const int msThresholdToConsiderSameChangeFromDifferentFileWatchers = 500;
            var isDuplicatedChangesComingFromDifferentFileWatcher = _dynamicFileHotReloadStateEntries
                .Any(f => f.FullFileName == filePathToUse
                          && (DateTime.UtcNow - f.FileChangedOn).TotalMilliseconds <
                          msThresholdToConsiderSameChangeFromDifferentFileWatchers);
            if (isDuplicatedChangesComingFromDifferentFileWatcher)
            {
                LoggerScoped.LogWarning(
                    $"FastScriptReload: Looks like change to: {filePathToUse} have already been added for processing. This can happen if you have multiple file watchers set in a way that they overlap.");
                return;
            }

            _dynamicFileHotReloadStateEntries.Add(new DynamicFileHotReloadState(filePathToUse, DateTime.UtcNow));
        }

        public void StartWatchingDirectoryAndSubdirectories(string directoryPath, string filter,
            bool includeSubdirectories)
        {
            foreach (var kv in FileWatcherTokensToResolvePathFn)
                directoryPath = directoryPath.Replace(kv.Key, kv.Value());

            var directoryInfo = new DirectoryInfo(directoryPath);

            if (!directoryInfo.Exists)
                LoggerScoped.LogWarning(
                    $"FastScriptReload: Directory: '{directoryPath}' does not exist, make sure file-watcher setup is correct. You can access via: Window -> Fast Script Reload -> File Watcher (Advanced Setup)");

            var fileWatcher = new FileSystemWatcher();

            fileWatcher.Path = directoryInfo.FullName;
            fileWatcher.IncludeSubdirectories = includeSubdirectories;
            fileWatcher.Filter = filter;
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileWatcher.Changed += OnWatchedFileChange;

            fileWatcher.EnableRaisingEvents = true;

            _fileWatchers.Add(fileWatcher);
        }

        ~FastScriptReloadManager()
        {
            LoggerScoped.LogDebug("Destroying FSR Manager ");
            if (_instance != null)
            {
                if (_lastPlayModeStateChange == PlayModeStateChange.EnteredPlayMode)
                    LoggerScoped.LogError(
                        "Manager is being destroyed in play session, this indicates some sort of issue where static variables were reset, hot reload will not function properly please reset. " +
                        "This is usually caused by Unity triggering that reset for some reason that's outside of asset control - other static variables will also be affected and recovering just hot reload would hide wider issue.");
                ClearFileWatchers();
            }
        }

        [MenuItem("Assets/Fast Script Reload/Add \\ Open User Script Rewrite Override", false,
            BaseMenuItemPriorityManualScriptOverride + 1)]
        public static void AddHotReloadManualScriptOverride()
        {
            if (Selection.activeObject is MonoScript script) ScriptGenerationOverridesManager.AddScriptOverride(script);
        }

        [MenuItem("Assets/Fast Script Reload/Add \\ Open User Script Rewrite Override", true)]
        public static bool AddHotReloadManualScriptOverrideValidateFn()
        {
            return Selection.activeObject is MonoScript;
        }

        [MenuItem("Assets/Fast Script Reload/Remove User Script Rewrite Override", false,
            BaseMenuItemPriorityManualScriptOverride + 2)]
        public static void RemoveHotReloadManualScriptOverride()
        {
            if (Selection.activeObject is MonoScript script)
                ScriptGenerationOverridesManager.TryRemoveScriptOverride(script);
        }

        [MenuItem("Assets/Fast Script Reload/Remove User Script Rewrite Override", true)]
        public static bool RemoveHotReloadManualScriptOverrideValidateFn()
        {
            if (Selection.activeObject is MonoScript script)
                return ScriptGenerationOverridesManager.TryGetScriptOverride(
                    new FileInfo(Path.Combine(Path.Combine(Application.dataPath + "//..",
                        AssetDatabase.GetAssetPath(script)))),
                    out _
                );

            return false;
        }

        [MenuItem("Assets/Fast Script Reload/Show User Script Rewrite Overrides", false,
            BaseMenuItemPriorityManualScriptOverride + 3)]
        public static void ShowManualScriptRewriteOverridesInUi()
        {
            var window = FastScriptReloadWelcomeScreen.Init();
            window.OpenUserScriptRewriteOverridesSection();
        }

        [MenuItem("Assets/Fast Script Reload/Add Hot-Reload Exclusion", false, BaseMenuItemPriorityExclusions + 1)]
        public static void AddFileAsExcluded()
        {
            FastScriptReloadPreference.FilesExcludedFromHotReload.AddElement(
                ResolveRelativeToAssetDirectoryFilePath(Selection.activeObject));
        }

        [MenuItem("Assets/Fast Script Reload/Add Hot-Reload Exclusion", true)]
        public static bool AddFileAsExcludedValidateFn()
        {
            return Selection.activeObject is MonoScript
                   && !(FastScriptReloadPreference.FilesExcludedFromHotReload.GetEditorPersistedValueOrDefault() as
                           IEnumerable<string> ?? Array.Empty<string>())
                       .Contains(ResolveRelativeToAssetDirectoryFilePath(Selection.activeObject));
        }

        [MenuItem("Assets/Fast Script Reload/Remove Hot-Reload Exclusion", false, BaseMenuItemPriorityExclusions + 2)]
        public static void RemoveFileAsExcluded()
        {
            FastScriptReloadPreference.FilesExcludedFromHotReload.RemoveElement(
                ResolveRelativeToAssetDirectoryFilePath(Selection.activeObject));
        }

        [MenuItem("Assets/Fast Script Reload/Remove Hot-Reload Exclusion", true)]
        public static bool RemoveFileAsExcludedValidateFn()
        {
            return Selection.activeObject is MonoScript
                   && (FastScriptReloadPreference.FilesExcludedFromHotReload.GetEditorPersistedValueOrDefault() as
                       IEnumerable<string> ?? Array.Empty<string>())
                   .Contains(ResolveRelativeToAssetDirectoryFilePath(Selection.activeObject));
        }

        [MenuItem("Assets/Fast Script Reload/Show Exclusions", false, BaseMenuItemPriorityExclusions + 3)]
        public static void ShowExcludedFilesInUi()
        {
            var window = FastScriptReloadWelcomeScreen.Init();
            window.OpenExclusionsSection();
        }


        private static string ResolveRelativeToAssetDirectoryFilePath(Object obj)
        {
            return AssetDatabase.GetAssetPath(obj.GetInstanceID());
        }

        public void Update()
        {
            _isEditorModeHotReloadEnabled = (bool)FastScriptReloadPreference.EnableExperimentalEditorHotReloadSupport
                .GetEditorPersistedValueOrDefault();
            if (_lastPlayModeStateChange == PlayModeStateChange.ExitingPlayMode && Instance._fileWatchers.Any())
                ClearFileWatchers();

            if (!_isEditorModeHotReloadEnabled && !EditorApplication.isPlaying) return;

            if (_isEditorModeHotReloadEnabled)
                EnsureInitialized();
            else if (_lastPlayModeStateChange == PlayModeStateChange.EnteredPlayMode) EnsureInitialized();
            // if (_lastPlayModeStateChange != PlayModeStateChange.ExitingPlayMode && Application.isPlaying && Instance._fileWatchers.Count == 0 && FastScriptReloadPreference.FileWatcherSetupEntries.GetElementsTyped().Count > 0)
            // {
            //     LoggerScoped.LogWarning("Reinitializing file-watchers as defined configuration does not match current instance setup. If hot reload still doesn't work you'll need to reset play session.");
            //     ClearFileWatchers();
            //     EnsureInitialized();
            // }
            AssignConfigValuesThatCanNotBeAccessedOutsideOfMainThread();

            if (!_assemblyChangesLoaderResolverResolutionAlreadyCalled)
            {
                AssemblyChangesLoaderResolver.Instance
                    .Resolve(); //WARN: need to resolve initially in case monobehaviour singleton is not created
                _assemblyChangesLoaderResolverResolutionAlreadyCalled = true;
            }

            if ((bool)FastScriptReloadPreference.EnableAutoReloadForChangedFiles.GetEditorPersistedValueOrDefault() &&
                (DateTime.UtcNow - _lastTimeChangeBatchRun).TotalSeconds > (int)FastScriptReloadPreference
                    .BatchScriptChangesAndReloadEveryNSeconds.GetEditorPersistedValueOrDefault())
                TriggerReloadForChangedFiles();
        }

        private static void ClearFileWatchers()
        {
            foreach (var fileWatcher in Instance._fileWatchers) fileWatcher.Dispose();

            Instance._fileWatchers.Clear();
        }

        private void AssignConfigValuesThatCanNotBeAccessedOutsideOfMainThread()
        {
            //TODO: PERF: needed in file watcher but when run on non-main thread causes exception. 
            _currentFileExclusions = FastScriptReloadPreference.FilesExcludedFromHotReload.GetElements();
            _triggerDomainReloadIfOverNDynamicallyLoadedAssembles = (int)FastScriptReloadPreference
                .TriggerDomainReloadIfOverNDynamicallyLoadedAssembles.GetEditorPersistedValueOrDefault();
            _isOnDemandHotReloadEnabled =
                (bool)FastScriptReloadPreference.EnableOnDemandReload.GetEditorPersistedValueOrDefault();
            EnableExperimentalThisCallLimitationFix = (bool)FastScriptReloadPreference
                .EnableExperimentalThisCallLimitationFix.GetEditorPersistedValueOrDefault();
            AssemblyChangesLoaderEditorOptionsNeededInBuild.UpdateValues(
                (bool)FastScriptReloadPreference.IsDidFieldsOrPropertyCountChangedCheckDisabled
                    .GetEditorPersistedValueOrDefault(),
                (bool)FastScriptReloadPreference.EnableExperimentalAddedFieldsSupport.GetEditorPersistedValueOrDefault()
            );
        }

        public void TriggerReloadForChangedFiles()
        {
            if (!Application.isPlaying &&
                _hotReloadPerformedCount > _triggerDomainReloadIfOverNDynamicallyLoadedAssembles)
            {
                LoggerScoped.LogWarning(
                    $"Dynamically created assembles reached over: {_triggerDomainReloadIfOverNDynamicallyLoadedAssembles} - triggering full domain reload to clean up. You can adjust that value in settings.");
#if UNITY_2019_3_OR_NEWER
                CompilationPipeline
                    .RequestScriptCompilation(); //TODO: add some timer to ensure this does not go into some kind of loop
#elif UNITY_2017_1_OR_NEWER
                 var editorAssembly = Assembly.GetAssembly(typeof(Editor));
                 var editorCompilationInterfaceType =
 editorAssembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface");
                 var dirtyAllScriptsMethod =
 editorCompilationInterfaceType.GetMethod("DirtyAllScripts", BindingFlags.Static | BindingFlags.Public);
                 dirtyAllScriptsMethod.Invoke(editorCompilationInterfaceType, null);
#endif
            }

            var assemblyChangesLoader = AssemblyChangesLoaderResolver.Instance.Resolve();
            var changesAwaitingHotReload = _dynamicFileHotReloadStateEntries
                .Where(e => e.IsAwaitingCompilation)
                .ToList();

            if (changesAwaitingHotReload.Any())
            {
                changesAwaitingHotReload.ForEach(c => { c.IsBeingProcessed = true; });

                var unityMainThreadDispatcher =
                    UnityMainThreadDispatcher.Instance
                        .EnsureInitialized(); //need to pass that in, resolving on other than main thread will cause exception
                Task.Run(() =>
                {
                    List<string> sourceCodeFilesWithUniqueChangesAwaitingHotReload = null;
                    try
                    {
                        sourceCodeFilesWithUniqueChangesAwaitingHotReload = changesAwaitingHotReload
                            .GroupBy(e => e.FullFileName)
                            .Select(e => e.First().FullFileName).ToList();

                        var dynamicallyLoadedAssemblyCompilerResult =
                            DynamicAssemblyCompiler.Compile(sourceCodeFilesWithUniqueChangesAwaitingHotReload,
                                unityMainThreadDispatcher);
                        if (!dynamicallyLoadedAssemblyCompilerResult.IsError)
                        {
                            changesAwaitingHotReload.ForEach(c =>
                            {
                                c.FileCompiledOn = DateTime.UtcNow;
                                c.AssemblyNameCompiledIn = dynamicallyLoadedAssemblyCompilerResult.CompiledAssemblyPath;
                            });

                            //TODO: return some proper results to make sure entries are correctly updated
                            assemblyChangesLoader.DynamicallyUpdateMethodsForCreatedAssembly(
                                dynamicallyLoadedAssemblyCompilerResult.CompiledAssembly,
                                AssemblyChangesLoaderEditorOptionsNeededInBuild);
                            changesAwaitingHotReload.ForEach(c =>
                            {
                                c.HotSwappedOn = DateTime.UtcNow;
                                c.IsBeingProcessed = false;
                            }); //TODO: technically not all were hot swapped at same time

                            _hotReloadPerformedCount++;
                        }
                        else
                        {
                            if (dynamicallyLoadedAssemblyCompilerResult.MessagesFromCompilerProcess.Count > 0)
                            {
                                var msg = new StringBuilder();
                                foreach (var message in dynamicallyLoadedAssemblyCompilerResult
                                             .MessagesFromCompilerProcess)
                                    msg.AppendLine(
                                        $"Error  when compiling, it's best to check code and make sure it's compilable \r\n {message}\n");

                                var errorMessage = msg.ToString();

                                changesAwaitingHotReload.ForEach(c =>
                                {
                                    c.ErrorOn = DateTime.UtcNow;
                                    c.ErrorText = errorMessage;
                                });

                                throw new Exception(errorMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerScoped.LogError(
                            $"Error when updating files: '{(sourceCodeFilesWithUniqueChangesAwaitingHotReload != null ? string.Join(",", sourceCodeFilesWithUniqueChangesAwaitingHotReload.Select(fn => new FileInfo(fn).Name)) : "unknown")}', {ex}");
                    }
                });
            }

            _lastTimeChangeBatchRun = DateTime.UtcNow;
        }

        private void OnEditorApplicationOnplayModeStateChanged(PlayModeStateChange obj)
        {
            Instance._lastPlayModeStateChange = obj;

            if ((bool)FastScriptReloadPreference.IsForceLockAssembliesViaCode.GetEditorPersistedValueOrDefault())
                if (obj == PlayModeStateChange.EnteredPlayMode)
                {
                    EditorApplication.LockReloadAssemblies();
                    _wasLockReloadAssembliesCalled = true;
                }

            if (obj == PlayModeStateChange.EnteredEditMode && _wasLockReloadAssembliesCalled)
            {
                EditorApplication.UnlockReloadAssemblies();
                _wasLockReloadAssembliesCalled = false;
            }
        }

        private static void EnsureInitialized()
        {
            if (!(bool)FastScriptReloadPreference.EnableAutoReloadForChangedFiles.GetEditorPersistedValueOrDefault()
                && !(bool)FastScriptReloadPreference.EnableOnDemandReload.GetEditorPersistedValueOrDefault())
            {
                if (!_hotReloadDisabledWarningMessageShownAlready)
                {
                    LoggerScoped.LogWarning(
                        "Both auto hot reload and on-demand reload are disabled, file watchers will not be initialized. Please adjust settings and restart if you want hot reload to work.");
                    _hotReloadDisabledWarningMessageShownAlready = true;
                }

                return;
            }

            if (Instance._fileWatchers.Count == 0)
            {
                var fileWatcherSetupEntries = FastScriptReloadPreference.FileWatcherSetupEntries.GetElementsTyped();
                if (fileWatcherSetupEntries.Count == 0)
                    LoggerScoped.LogWarning(
                        "There are no file watcher setup entries. Tool will not be able to pick changes automatically");

                foreach (var fileWatcherSetupEntry in fileWatcherSetupEntries)
                    Instance.StartWatchingDirectoryAndSubdirectories(fileWatcherSetupEntry.path,
                        fileWatcherSetupEntry.filter, fileWatcherSetupEntry.includeSubdirectories);
            }
        }
    }

    public class DynamicFileHotReloadState
    {
        public DynamicFileHotReloadState(string fullFileName, DateTime fileChangedOn)
        {
            FullFileName = fullFileName;
            FileChangedOn = fileChangedOn;
        }

        public string FullFileName { get; set; }
        public DateTime FileChangedOn { get; set; }
        public bool IsAwaitingCompilation => !IsFileCompiled && !ErrorOn.HasValue && !IsBeingProcessed;
        public bool IsFileCompiled => FileCompiledOn.HasValue;
        public DateTime? FileCompiledOn { get; set; }

        public string AssemblyNameCompiledIn { get; set; }

        public bool IsAwaitingHotSwap => IsFileCompiled && !HotSwappedOn.HasValue;
        public DateTime? HotSwappedOn { get; set; }
        public bool IsChangeHotSwapped { get; set; }

        public string ErrorText { get; set; }
        public DateTime? ErrorOn { get; set; }
        public bool IsBeingProcessed { get; set; }
    }
}