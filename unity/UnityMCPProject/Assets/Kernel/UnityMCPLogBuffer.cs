using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMCP.Kernel
{
    [InitializeOnLoad]
    public static class UnityMCPLogBuffer
    {
        private const int MAX_LOGS = 2000;
        
        // ConcurrentQueue is thread-safe for adding/iterating.
        // We will manually manage the size by dequeueing if too large.
        // Since it's a concurrent queue, we might slightly exceed MAX_LOGS temporarily, which is fine.
        private static readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();

        public struct LogEntry
        {
            public string timestamp;
            public string type;
            public string message;
            public string stackTrace;
        }

        static UnityMCPLogBuffer()
        {
            // Register callback on editor load
            Application.logMessageReceivedThreaded -= HandleLog;
            Application.logMessageReceivedThreaded += HandleLog;
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                type = type.ToString(),
                message = condition,
                stackTrace = stackTrace
            };

            _logQueue.Enqueue(entry);

            // Trim if needed (simple check)
            if (_logQueue.Count > MAX_LOGS)
            {
                _logQueue.TryDequeue(out _);
            }
        }

        public static List<LogEntry> GetRecent(int count)
        {
            if (count < 1) count = 1;

            var allLogs = _logQueue.ToArray();
            int total = allLogs.Length;
            
            // If requested count is greater than total, return all.
            // Otherwise, return the last 'count' items.
            int startIndex = Mathf.Max(0, total - count);
            int amount = total - startIndex;

            var result = new List<LogEntry>(amount);
            for (int i = startIndex; i < total; i++)
            {
                result.Add(allLogs[i]);
            }
            return result;
        }
        public static void Clear()
        {
            // ConcurrentQueue does not have Clear(), so we dequeue all
            while (_logQueue.TryDequeue(out _)) { }
        }
    }
}
