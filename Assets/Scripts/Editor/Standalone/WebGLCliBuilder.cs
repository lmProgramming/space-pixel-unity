#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ZLinq;

namespace Editor.Standalone
{
    /// <summary>
    ///     Entry point for headless CI builds. Invoked by unity-builder through -executeMethod.
    /// </summary>
    public static class WebGLCliBuilder
    {
        private static readonly string OutputDirectory =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "build", "WebGL"));

        // GitHub Pages cannot send Content-Encoding headers, so compressed WebGL payloads would
        // be downloaded as opaque files and break the loader. Build uncompressed instead.
        private const WebGLCompressionFormat PagesCompatibleCompression = WebGLCompressionFormat.Disabled;

        public static void Build()
        {
            PlayerSettings.WebGL.compressionFormat = PagesCompatibleCompression;

            var scenes = EditorBuildSettings.scenes.AsValueEnumerable()
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("[WebGLCliBuilder] No enabled scenes in EditorBuildSettings.");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception(
                    $"[WebGLCliBuilder] WebGL build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        }
    }
}
#endif