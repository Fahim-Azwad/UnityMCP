using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEditor;

namespace UnityMCP.Kernel
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class UnityMCPMainThread
    {
        private sealed class Job
        {
            public Func<object> action;
            public ManualResetEventSlim waitHandle;
            public object result;
            public Exception error;
        }

        private static readonly ConcurrentQueue<Job> _jobs = new ConcurrentQueue<Job>();

        static UnityMCPMainThread()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            while (_jobs.TryDequeue(out Job job))
            {
                try
                {
                    job.result = job.action();
                }
                catch (Exception e)
                {
                    job.error = e;
                }
                finally
                {
                    job.waitHandle.Set();
                }
            }
        }

        /// <summary>
        /// Runs a job on the main thread and waits for the result.
        /// </summary>
        public static T Run<T>(Func<T> action, int timeoutMs = 2000)
        {
            using (var handle = new ManualResetEventSlim(false))
            {
                var job = new Job
                {
                    action = () => action(),
                    waitHandle = handle
                };

                _jobs.Enqueue(job);

                if (!handle.Wait(timeoutMs))
                    throw new TimeoutException("Main thread dispatch timed out.");

                if (job.error != null) throw job.error;

                return (T)job.result;
            }
        }
    }
#endif
}
