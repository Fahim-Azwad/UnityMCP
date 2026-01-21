using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMCP.Kernel
{
#if UNITY_EDITOR
    public static class UnityMCPSnapshotBuilder
    {
        public static string BuildSnapshotJson()
        {
            // MUST run on main thread
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            var objects = new List<Node>(1024);

            foreach (var root in roots)
            {
                Traverse(root.transform, objects, maxObjects: 3000);
                if (objects.Count >= 3000) break;
            }

            // Manual JSON (safe, no external libs)
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"scene\":").Append(JsonStr(scene.name)).Append(",");
            sb.Append("\"truncated\":").Append(objects.Count >= 3000 ? "true" : "false").Append(",");
            sb.Append("\"objects\":[");

            for (int i = 0; i < objects.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(objects[i].ToJson());
            }

            sb.Append("]}");
            var json = sb.ToString();
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[UnityMCP] SnapshotBuilder returned empty JSON. Returning {} instead.");
                return "{}";
            }
            return json;
        }

        private static void Traverse(Transform t, List<Node> outList, int maxObjects)
        {
            if (outList.Count >= maxObjects) return;

            var go = t.gameObject;
            var comps = go.GetComponents<Component>();

            var compNames = new List<string>(comps.Length);
            foreach (var c in comps)
            {
                if (c == null) continue;
                compNames.Add(c.GetType().Name);
            }

            outList.Add(new Node
            {
                path = GetPath(t),
                active = go.activeSelf,
                tag = go.tag,
                layer = go.layer,
                position = t.position,
                rotation = t.rotation,
                scale = t.localScale,
                components = compNames
            });

            for (int i = 0; i < t.childCount; i++)
            {
                Traverse(t.GetChild(i), outList, maxObjects);
                if (outList.Count >= maxObjects) return;
            }
        }

        private static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            var cur = t;
            while (cur != null)
            {
                stack.Push(cur.name);
                cur = cur.parent;
            }
            return string.Join("/", stack.ToArray());
        }

        private static string JsonStr(string s)
        {
            if (s == null) return "\"\"";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private struct Node
        {
            public string path;
            public bool active;
            public string tag;
            public int layer;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public List<string> components;

            public string ToJson()
            {
                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"path\":").Append(JsonStr(path)).Append(",");
                sb.Append("\"active\":").Append(active ? "true" : "false").Append(",");
                sb.Append("\"tag\":").Append(JsonStr(tag)).Append(",");
                sb.Append("\"layer\":").Append(layer).Append(",");
                sb.Append("\"position\":").Append(Vec3(position)).Append(",");
                sb.Append("\"rotation\":").Append(Quat(rotation)).Append(",");
                sb.Append("\"scale\":").Append(Vec3(scale)).Append(",");
                sb.Append("\"components\":").Append(StrArray(components));
                sb.Append("}");
                return sb.ToString();
            }

            private static string Vec3(Vector3 v) => $"[{v.x},{v.y},{v.z}]";
            private static string Quat(Quaternion q) => $"[{q.x},{q.y},{q.z},{q.w}]";

            private static string StrArray(List<string> arr)
            {
                if (arr == null) return "[]";
                var sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < arr.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(JsonStr(arr[i]));
                }
                sb.Append("]");
                return sb.ToString();
            }

            private static string JsonStr(string s)
            {
                if (s == null) return "\"\"";
                return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            }
        }
    }
#endif
}
