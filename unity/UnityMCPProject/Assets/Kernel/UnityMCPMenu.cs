using UnityEditor;
using UnityEngine;

namespace UnityMCP.Kernel
{
    public static class UnityMCPMenu
    {
#if UNITY_EDITOR
        [MenuItem("Tools/UnityMCP/Start Server")]
        public static void StartServer()
        {
            EditorPrefs.SetBool("UnityMCP_AutoRestart", true);
            UnityMCPServer.Start();
        }

        [MenuItem("Tools/UnityMCP/Start Server", true)]
        public static bool ValidateStartServer()
        {
            return !UnityMCPServer.IsRunning;
        }

        [MenuItem("Tools/UnityMCP/Stop Server", true)]
        public static bool ValidateStopServer()
        {
            return UnityMCPServer.IsRunning;
        }

        [MenuItem("Tools/UnityMCP/Stop Server")]
        public static void StopServer()
        {
            EditorPrefs.SetBool("UnityMCP_AutoRestart", false);
            UnityMCPServer.Stop();
        }
#endif
    }
}
