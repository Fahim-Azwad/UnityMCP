using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityMCP.Kernel
{
#if UNITY_EDITOR
    
    public static class UnityMCPEndpoints
    {
        public static void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string method = request.HttpMethod?.ToUpperInvariant() ?? "GET";
            string path = request.Url != null ? request.Url.AbsolutePath : "/";
            path = path.TrimEnd('/');
            if (string.IsNullOrEmpty(path)) path = "/";

            int statusCode = 200;
            string jsonResponse;

            try
            {
                if (method == "GET" && path == "/logs/recent")
                {
                    int count = 200;
                    string countStr = request.QueryString["count"];
                    if (!string.IsNullOrEmpty(countStr))
                    {
                        if (int.TryParse(countStr, out int c))
                        {
                            count = Mathf.Clamp(c, 1, 2000);
                        }
                    }

                    var logs = UnityMCPLogBuffer.GetRecent(count);

                    // Manually serialize logs to JSON
                    var sb = new StringBuilder();
                    sb.Append("{\"logs\":[");
                    for (int i = 0; i < logs.Count; i++)
                    {
                        var entry = logs[i];
                        sb.Append("{");
                        sb.Append($"\"timestamp\":\"{entry.timestamp}\",");
                        sb.Append($"\"type\":\"{entry.type}\",");
                        sb.Append($"\"message\":\"{EscapeJsonString(entry.message)}\",");
                        sb.Append($"\"stackTrace\":\"{EscapeJsonString(entry.stackTrace)}\"");
                        sb.Append("}");
                        if (i < logs.Count - 1) sb.Append(",");
                    }
                    sb.Append("]}");

                    jsonResponse = CreateResponse(true, sb.ToString(), null);
                }
                else if (method == "GET" && path == "/logs/compiler")
                {
                    int count = 200;
                    string countStr = request.QueryString["count"];
                    if (!string.IsNullOrEmpty(countStr) && int.TryParse(countStr, out int c))
                    {
                        count = Mathf.Clamp(c, 1, 2000);
                    }

                    var logs = UnityMCPLogBuffer.GetRecent(count);
                    var sb = new StringBuilder();
                    sb.Append("{\"compilerErrors\":[");

                    bool first = true;
                    for (int i = 0; i < logs.Count; i++)
                    {
                        var entry = logs[i];
                        bool isCompilerError = 
                            entry.type == "Error" && (
                            entry.message.Contains("error CS") || 
                            entry.message.Contains("Compilation failed") ||
                            (entry.message.Contains("Assets/") && entry.message.Contains(".cs("))
                            );

                        if (isCompilerError)
                        {
                            if (!first) sb.Append(",");
                            first = false;

                            sb.Append("{");
                            sb.Append($"\"timestamp\":\"{entry.timestamp}\",");
                            sb.Append($"\"message\":\"{EscapeJsonString(entry.message)}\",");
                            sb.Append($"\"stackTrace\":\"{EscapeJsonString(entry.stackTrace)}\"");
                            sb.Append("}");
                        }
                    }
                    sb.Append("]}");

                    jsonResponse = CreateResponse(true, sb.ToString(), null);
                }
                else if (method == "GET" && path == "/state/snapshot")
                {
                    try
                    {
                        // IMPORTANT: absolutely no Unity calls above this line.
                        var snapshotJson = UnityMCPMainThread.Run(() =>
                        {
                            // All Unity calls must be inside here
                            return UnityMCPSnapshotBuilder.BuildSnapshotJson();
                        }, timeoutMs: 3000);
                        
                        Debug.Log($"[UnityMCP] Snapshot length: {snapshotJson?.Length ?? 0}");

                        if (string.IsNullOrWhiteSpace(snapshotJson))
                        {
                            snapshotJson = "{}";
                        }

                        jsonResponse = CreateResponse(true, $"{{\"snapshot\":{snapshotJson}}}", null);
                    }
                    catch (Exception ex)
                    {
                        statusCode = 500;
                        jsonResponse = CreateResponse(false, "{}", new[] { "Snapshot failed: " + ex.Message });
                    }
                }
                else if (method == "POST" && path == "/command/apply")
                {
                    Debug.Log("[UnityMCP] /command/apply hit");

                    string body;
                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
                    {
                        body = reader.ReadToEnd();
                    }

                    Debug.Log($"[UnityMCP] /command/apply body received (len={body.Length})");

                    CommandRequest cmdRequest = null;
                    try
                    {
                        cmdRequest = UnityMCPCommandParser.Parse(body);
                        if (cmdRequest == null)
                            throw new Exception("Failed to parse command request JSON");
                    }
                    catch (Exception e)
                    {
                        statusCode = 400;
                        jsonResponse = CreateResponse(false, "{}", new[] { "Invalid JSON: " + e.Message });
                        goto WRITE_RESPONSE;
                    }

                    Debug.Log($"[UnityMCP] Executing {cmdRequest.commands?.Count ?? 0} commands");

                    try
                    {
                        var result = UnityMCPMainThread.Run(() =>
                        {
                            return UnityMCPCommandExecutor.Execute(cmdRequest);
                        }, 5000);

                        // Construct response with requestId wrapper
                        var sb = new StringBuilder();
                        sb.Append("{");
                        if (!string.IsNullOrEmpty(cmdRequest.requestId))
                            sb.Append($"\"requestId\":\"{EscapeJsonString(cmdRequest.requestId)}\",");
                        sb.Append($"\"created\":{result}");
                        sb.Append("}");

                        jsonResponse = CreateResponse(true, sb.ToString(), null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[UnityMCP] Command execution failed: " + e);
                        statusCode = 500;
                        jsonResponse = CreateResponse(false, "{}", new[] { e.Message });
                    }
                }
                else if (method == "POST" && path == "/play/run")
                {
                    string body;
                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
                    {
                        body = reader.ReadToEnd();
                    }

                    int playSeconds = 2;
                    var dict = UnityMCPCommandParser.MiniParse(body) as Dictionary<string, object>;
                    if (dict != null && dict.ContainsKey("seconds"))
                    {
                         playSeconds = (int)Convert.ToSingle(dict["seconds"], CultureInfo.InvariantCulture);
                    }
                    if (playSeconds < 1) playSeconds = 1;

                    // 1. Clear old result on main thread
                    UnityMCPMainThread.Run(() => { SessionState.SetString("UnityMCP_PlayResult", ""); return true; });

                    // 2. Start background task
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try 
                        {
                            DateTime startTime = DateTime.UtcNow;
                            
                            // Start Play
                            UnityMCPMainThread.Run(() => EditorApplication.isPlaying = true);
                            
                            // Wait
                            try
                            {
                                System.Threading.Thread.Sleep(playSeconds * 1000);
                            }
                            catch (System.Threading.ThreadAbortException)
                            {
                                System.Threading.Thread.ResetAbort();
                                return;
                            }
                            
                            // Stop Play
                            UnityMCPMainThread.Run(() => EditorApplication.isPlaying = false);
                            
                            // Collect Logs
                            var logs = UnityMCPLogBuffer.GetRecent(500);
                            var sb = new StringBuilder();
                            sb.Append("[");
                            bool first = true;

                            foreach(var entry in logs)
                            {
                                if (DateTime.TryParse(entry.timestamp, null, DateTimeStyles.RoundtripKind, out DateTime t))
                                {
                                    if (t >= startTime && (entry.type == "Exception" || entry.type == "Error"))
                                    {
                                        if (!first) sb.Append(",");
                                        first = false;

                                        sb.Append("{");
                                        sb.Append($"\"timestamp\":\"{entry.timestamp}\",");
                                        sb.Append($"\"type\":\"{entry.type}\",");
                                        sb.Append($"\"message\":\"{EscapeJsonString(entry.message)}\",");
                                        sb.Append($"\"stackTrace\":\"{EscapeJsonString(entry.stackTrace)}\"");
                                        sb.Append("}");
                                    }
                                }
                            }
                            sb.Append("]");
                            string resultJson = sb.ToString();

                            // Store Result
                            UnityMCPMainThread.Run(() => { SessionState.SetString("UnityMCP_PlayResult", resultJson); return true; });
                        }
                        catch (System.Threading.ThreadAbortException)
                        {
                            System.Threading.Thread.ResetAbort();
                            Debug.LogWarning("[UnityMCP] Async Play runner aborted (domain reload). This is expected.");
                        }
                        catch(Exception e)
                        {
                             Debug.LogError("[UnityMCP] Async Play runner failed: " + e);
                        }
                    });

                    // 3. Return accepted
                    jsonResponse = CreateResponse(true, "{\"started\":true}", null);
                }
                else if (method == "GET" && path == "/play/last")
                {
                     string resultJson = UnityMCPMainThread.Run(() => SessionState.GetString("UnityMCP_PlayResult", ""));
                     if (string.IsNullOrEmpty(resultJson)) resultJson = "[]";
                     jsonResponse = CreateResponse(true, $"{{\"exceptions\":{resultJson}}}", null);
                }
                else if (method == "POST" && path == "/scripts/write")
                {
                    try
                    {
                        string body;
                        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
                        {
                            body = reader.ReadToEnd();
                        }

                        var scriptReq = JsonUtility.FromJson<ScriptWriteRequest>(body);
                        if (scriptReq == null || string.IsNullOrEmpty(scriptReq.path) || string.IsNullOrEmpty(scriptReq.content))
                        {
                            throw new Exception("Invalid request: path and content are required.");
                        }

                        // Write script using the secure writer
                        string createdPath = UnityMCPScriptWriter.WriteScript(scriptReq.path, scriptReq.content);

                        jsonResponse = CreateResponse(true, $"{{\"assetPath\":\"{EscapeJsonString(createdPath)}\"}}", null);
                    }
                    catch (Exception ex)
                    {
                        statusCode = 500;
                        jsonResponse = CreateResponse(false, "{}", new[] { ex.Message });
                    }
                }
                else if (method == "GET" && path == "/compiler/errors")
                {
                    string sinceStr = request.QueryString["since"];
                    string clearStr = request.QueryString["clear"];
                    
                    DateTime since = DateTime.MinValue;
                    if (!string.IsNullOrEmpty(sinceStr))
                    {
                        DateTime.TryParse(sinceStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out since);
                    }

                    var logs = UnityMCPLogBuffer.GetRecent(2000);
                    var sb = new StringBuilder();
                    sb.Append("{\"errors\":[");

                    bool first = true;
                    foreach (var entry in logs)
                    {
                        if (DateTime.TryParse(entry.timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime entryTime))
                        {
                            if (entryTime <= since) continue;
                        }

                        bool isCompilerError = entry.type == "Error" && (
                            entry.message.Contains("error CS") || 
                            entry.message.Contains("Compilation failed") || 
                            (entry.message.Contains("Assets/") && entry.message.Contains(".cs("))
                        );

                        if (isCompilerError)
                        {
                            if (!first) sb.Append(",");
                            first = false;

                            sb.Append("{");
                            sb.Append($"\"timestamp\":\"{entry.timestamp}\",");
                            sb.Append($"\"message\":\"{EscapeJsonString(entry.message)}\",");
                            sb.Append($"\"stackTrace\":\"{EscapeJsonString(entry.stackTrace)}\"");
                            sb.Append("}");
                        }
                    }
                    sb.Append("]}");

                    if (clearStr == "true")
                    {
                         UnityMCPLogBuffer.Clear();
                    }

                    jsonResponse = CreateResponse(true, sb.ToString(), null);
                }
                else
                {
                    statusCode = 404;
                    jsonResponse = CreateResponse(false, "{}", new[] { "Endpoint not found" });
                }
            }
            catch (Exception e)
            {
                statusCode = 500;
                jsonResponse = CreateResponse(false, "{}", new[] { e.Message });
            }

            WRITE_RESPONSE:
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnityMCP] Response write failed: {e.Message}");
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { }
                try { response.Close(); } catch { }
            }
        }

        [Serializable]
        private class ScriptWriteRequest
        {
            public string path;
            public string content;
        }

        private static string CreateResponse(bool ok, string validJsonData, string[] errors)
        {
            if (string.IsNullOrEmpty(validJsonData))
                validJsonData = "{}";

            string errorsJson = "[]";
            if (errors != null && errors.Length > 0)
            {
                var sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < errors.Length; i++)
                {
                    sb.Append("\"").Append(EscapeJsonString(errors[i])).Append("\"");
                    if (i < errors.Length - 1) sb.Append(",");
                }
                sb.Append("]");
                errorsJson = sb.ToString();
            }

            return $"{{\"ok\":{(ok ? "true" : "false")},\"data\":{validJsonData},\"errors\":{errorsJson}}}";
        }


        private static string EscapeJsonString(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
#endif
}
