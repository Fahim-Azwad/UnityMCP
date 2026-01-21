using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityMCP.Kernel
{
#if UNITY_EDITOR
    public static class UnityMCPCommandExecutor
    {
        private static Dictionary<string, GameObject> _aliasMap = new Dictionary<string, GameObject>();

        public static string Execute(CommandRequest request)
        {
            _aliasMap.Clear(); 
            var createdIds = new List<string>(); // Track created IDs for final path resolution
            
            Debug.Log($"[UnityMCP] Executing {request.commands.Count} commands");

            // 1. Execute all commands
            foreach (var cmd in request.commands)
            {
                Debug.Log($"[UnityMCP] Command: {cmd.type}");
                try
                {
                    ProcessCommand(cmd, createdIds);
                }
                catch (Exception e)
                {
                    throw new Exception($"Command '{cmd.type}' failed: {e.Message}");
                }
            }

            // 2. Resolve final paths for response
            var createdInfo = new StringBuilder();
            createdInfo.Append("{");
            bool first = true;

            foreach (var id in createdIds)
            {
                if (_aliasMap.TryGetValue(id, out GameObject go) && go != null)
                {
                    if (!first) createdInfo.Append(",");
                    first = false;

                    string finalPath = GetPath(go.transform);
                    createdInfo.Append($"\"{Escape(id)}\":{{");
                    createdInfo.Append($"\"instanceId\":{go.GetInstanceID()},");
                    createdInfo.Append($"\"path\":\"{Escape(finalPath)}\"");
                    createdInfo.Append("}");
                }
            }

            createdInfo.Append("}");
            return createdInfo.ToString();
        }

        private static void ProcessCommand(CommandData cmd, List<string> createdIds)
        {
             GameObject targetGO = null;

            switch (cmd.type)
            {
                case "CreatePrimitive":
                {
                    var pType = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), cmd.primitive, true);
                    targetGO = GameObject.CreatePrimitive(pType);
                    targetGO.name = !string.IsNullOrEmpty(cmd.name) ? cmd.name : pType.ToString();

                    if (cmd.position != null) targetGO.transform.position = cmd.position.Value;
                    if (cmd.rotation != null) targetGO.transform.rotation = cmd.rotation.Value;
                    if (cmd.scale != null) targetGO.transform.localScale = cmd.scale.Value;
                    
                    RegisterAlias(cmd.id, targetGO, createdIds);
                    break;
                }
                case "CreateEmpty":
                {
                    targetGO = new GameObject(!string.IsNullOrEmpty(cmd.name) ? cmd.name : "Empty");
                    if (cmd.position != null) targetGO.transform.position = cmd.position.Value;

                    RegisterAlias(cmd.id, targetGO, createdIds);
                    break;
                }
                case "Rename":
                {
                    targetGO = ResolveTarget(cmd.target);
                    targetGO.name = cmd.name;
                    break;
                }
                case "SetTransform":
                {
                    targetGO = ResolveTarget(cmd.target);
                    if (cmd.position != null) targetGO.transform.position = cmd.position.Value;
                    if (cmd.rotation != null) targetGO.transform.rotation = cmd.rotation.Value;
                    if (cmd.scale != null) targetGO.transform.localScale = cmd.scale.Value;
                    break;
                }
                case "AddComponent":
                {
                    targetGO = ResolveTarget(cmd.target);
                    // 1. Try UnityEngine namespace
                    var type = Type.GetType("UnityEngine." + cmd.componentType + ", UnityEngine");
                    // 2. Try direct type lookup
                    if (type == null) type = Type.GetType(cmd.componentType);
                    // 3. Try searching all assemblies (for user scripts)
                    if (type == null)
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = asm.GetType(cmd.componentType);
                            if (type != null) break;
                        }
                    }
                    
                    if (type == null) throw new Exception("Unknown component: " + cmd.componentType);
                    targetGO.AddComponent(type);
                    break;
                }
                case "AttachScript":
                {
                    targetGO = ResolveTarget(cmd.target);
                    Type type = null;
                    if (!string.IsNullOrEmpty(cmd.scriptClass))
                        type = Type.GetType(cmd.scriptClass);
                    
                    if (type == null && !string.IsNullOrEmpty(cmd.scriptClass))
                        type = Type.GetType("UnityMCP.Generated." + cmd.scriptClass);

                    if (type == null && !string.IsNullOrEmpty(cmd.scriptClass))
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = asm.GetType(cmd.scriptClass);
                            if (type != null) break;
                        }
                    }

                    if (type == null) throw new Exception("Script class not found: " + cmd.scriptClass);
                    targetGO.AddComponent(type);
                    break;
                }
                case "DestroyByName":
                {
                    if (string.IsNullOrEmpty(cmd.name)) throw new Exception("DestroyByName requires 'name'");
                    
                    // Helper to check if GO is valid scene object (not asset)
                    bool IsSceneObject(GameObject obj) => 
                        obj != null && !UnityEditor.EditorUtility.IsPersistent(obj) && 
                         (obj.hideFlags & HideFlags.NotEditable) == 0; // Filter out internal editor objects

                    if (cmd.all)
                    {
                        // heavily robust find including inactive
                        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                        int destroyed = 0;
                        foreach (var go in allObjects)
                        {
                            if (go.name == cmd.name && IsSceneObject(go))
                            {
                                GameObject.DestroyImmediate(go);
                                destroyed++;
                            }
                        }
                    }
                    else
                    {
                        // Standard Find only gets active objects. 
                        // If we want to be strict, we check if there are any inactive ones? 
                        // For non-all, standard Find is usually expected behavior, 
                        // but sticking to "active" is safer for simple logic unless requested.
                        // However, to fix "duplicates", "all" is the key.
                        // If single delete is requested, we stick to finding active first.
                        var go = GameObject.Find(cmd.name);
                        if (go != null)
                        {
                            GameObject.DestroyImmediate(go);
                        }
                    }
                    break;
                }
                default:
                    throw new Exception($"Unknown command type: {cmd.type}");
            }
        }

        private static void RegisterAlias(string id, GameObject go, List<string> createdIds)
        {
            if (!string.IsNullOrEmpty(id) && go != null)
            {
                _aliasMap[id] = go;
                createdIds.Add(id);
            }
        }

        private static GameObject ResolveTarget(string target)
        {
            if (string.IsNullOrEmpty(target)) throw new Exception("Target cannot be empty");

            // 1. Check alias
            if (_aliasMap.ContainsKey(target)) return _aliasMap[target];

            // 2. Check path (find)
            var go = GameObject.Find(target);
            if (go != null) return go;

            throw new Exception($"Target not found: {target}");
        }

        private static string GetPath(Transform t)
        {
            if (t.parent == null) return t.name;
            return GetPath(t.parent) + "/" + t.name;
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
#endif
}
