using ImmersiveVrToolsCommon.Runtime.Logging;
using UnityEditor;
using UnityEngine;

namespace FastScriptReload.Scripts.Runtime
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class LoggerScopedInitializer
    {
        static LoggerScopedInitializer()
        {
            Init();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            LoggerScoped.LogPrefix = "FSR: ";
        }
    }
}