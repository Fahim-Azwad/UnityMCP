#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityMCP.Generated.Editor
{
    [InitializeOnLoad]
    public static class UnityMCPAutoRestart
    {
        private const string Key = "UnityMCP_AutoRestart";

        static UnityMCPAutoRestart()
        {
            // Delay so Unity finishes domain reload cleanly
            EditorApplication.delayCall += TryRestart;
        }

        private static void TryRestart()
        {
            if (!EditorPrefs.GetBool(Key, false)) return;

            try
            {
                // We access the kernel server directly
                UnityMCP.Kernel.UnityMCPServer.Start();
                Debug.Log("[UnityMCP] Auto-restarted server after domain reload.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UnityMCP] Auto-restart failed: " + e);
            }
        }

        // helper endpoints can toggle this via EditorPrefs later
        public static void EnableAutoRestart(bool enabled) =>
            EditorPrefs.SetBool(Key, enabled);
    }
}
#endif
