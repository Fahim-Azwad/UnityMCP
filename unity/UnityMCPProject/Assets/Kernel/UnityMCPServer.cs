using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnityMCP.Kernel
{
    public static class UnityMCPServer
    {
#if UNITY_EDITOR
        private const int PORT = 7777;
        // private const string SESSION_KEY_RUNNING = "UnityMCP_ServerRunning"; // Removed in favor of EditorPrefs
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static bool _isRunning;

        public static bool IsRunning => _isRunning;

        public static void Start()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[UnityMCP] Server is already running.");
                return;
            }

            try
            {
                Debug.Log("[UnityMCP] Starting server...");

                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{PORT}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{PORT}/");
                
                Debug.Log("[UnityMCP] Prefixes added. Calling Start()...");
                
                _listener.Start();

                Debug.Log("[UnityMCP] HttpListener.Start() successful.");

                _isRunning = true;
                _serverThread = new Thread(ServerLoop);
                _serverThread.Start();
                
                // SessionState.SetBool(SESSION_KEY_RUNNING, true); // Managed by EditorPrefs now

                Debug.Log($"[UnityMCP] Server started at http://127.0.0.1:{PORT}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityMCP] Failed to start server: {e.Message}");
                Stop();
            }
        }

        public static void Stop()
        {
            _isRunning = false;
            // SessionState.SetBool(SESSION_KEY_RUNNING, false); // Managed by EditorPrefs now

            try
            {
                if (_listener != null)
                {
                    // Stop breaks GetContext() cleanly
                    if (_listener.IsListening) _listener.Stop();
                    _listener.Close();
                    _listener = null;
                }

            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnityMCP] Listener shutdown warning: {e.Message}");
            }

            if (_serverThread != null)
            {
                if (!_serverThread.Join(500))
                {
                    Debug.LogWarning("[UnityMCP] Server thread did not exit in time (skipping abort).");
                }
                _serverThread = null;
            }

            Debug.Log("[UnityMCP] Server stopped.");
        }

        private static void ServerLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem((_) => UnityMCPEndpoints.ProcessRequest(context));
                }
                catch (HttpListenerException) { return; }
                catch (ThreadAbortException) { return; }
                catch (Exception e)
                {
                    Debug.LogError($"[UnityMCP] Server loop error: {e.Message}");
                }
            }
        }
#endif
    }
}
