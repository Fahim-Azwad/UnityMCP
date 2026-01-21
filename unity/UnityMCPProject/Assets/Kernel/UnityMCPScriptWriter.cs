using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityMCP.Kernel
{
#if UNITY_EDITOR
    public static class UnityMCPScriptWriter
    {
        private const string GENERATED_ROOT = "Assets/Generated";

        public static string WriteScript(string relativePath, string content)
        {
            // 1. Path Safety Validation
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new Exception("Path cannot be empty");

            if (relativePath.Contains("..") || relativePath.Contains(":") || Path.IsPathRooted(relativePath))
                throw new Exception($"Invalid path '{relativePath}'. Paths must be relative and cannot contain traversal characters.");

            // 2. Resolve Full Path
            // We use standard slash for consistency in Unity
            relativePath = relativePath.Replace('\\', '/').TrimStart('/');
            string finalAssetPath = $"{GENERATED_ROOT}/{relativePath}";
            string fullSystemPath = Path.Combine(Application.dataPath, "Generated", relativePath);

            // Double check traversal didn't escape (e.g. if Path.Combine behavior varies)
            // Application.dataPath is <Project>/Assets
            // We expect <Project>/Assets/Generated/...
            if (!fullSystemPath.Replace('\\', '/').Contains("/Assets/Generated/"))
            {
                 throw new Exception($"Security Check Failed: Resolved path '{fullSystemPath}' is outside '{GENERATED_ROOT}'");
            }

            // 3. Create Directory
            string directory = Path.GetDirectoryName(fullSystemPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 4. Write File
            File.WriteAllText(fullSystemPath, content);

            // 5. Trigger Recompilation on Main Thread
            UnityMCPMainThread.Run(() =>
            {
                AssetDatabase.ImportAsset(finalAssetPath, ImportAssetOptions.ForceUpdate);
                // We use ImportAsset for specific file to be faster/safer than full Refresh, 
                // but usually for scripts to compile, Unity auto-detects. 
                // However, user requested AssetDatabase.Refresh() specifically in constraints.
                // Let's stick to simple Refresh per request or Import?
                // Plan said: "Call AssetDatabase.Refresh() on main thread"
                AssetDatabase.Refresh();
                return true;
            });

            return finalAssetPath;
        }
    }
#endif
}
