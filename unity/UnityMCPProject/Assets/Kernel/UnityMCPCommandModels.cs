using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UnityMCP.Kernel
{
#if UNITY_EDITOR
    // Simple command structures
    public class CommandRequest
    {
        public string requestId;
        public List<CommandData> commands = new List<CommandData>();
    }

    public class CommandData
    {
        public string type;
        // Creation fields
        public string id;       // Local alias
        public string primitive; // PrimitiveType name
        public string name;     // GameObject name
        
        // Target fields
        public string target;   // Alias or Path

        // Transform fields
        public Vector3? position;
        public Quaternion? rotation;
        public Vector3? scale;

        // Component fields
        public string componentType;
        public string scriptClass; // For AttachScript

        // Destruction fields
        public bool all; // For DestroyByName
    }

    /// <summary>
    /// Minimal JSON Parser to handle the specific requirements (like arrays for vectors)
    /// which JsonUtility does not support.
    /// </summary>
    public static class UnityMCPCommandParser
    {
        public static CommandRequest Parse(string json)
        {
            var req = new CommandRequest();
            var dict = MiniParse(json) as Dictionary<string, object>;
            
            if (dict == null) return req;

            if (dict.ContainsKey("requestId")) 
                req.requestId = dict["requestId"] as string;

            if (dict.ContainsKey("commands") && dict["commands"] is List<object> cmdList)
            {
                foreach (var cmdObj in cmdList)
                {
                    if (cmdObj is Dictionary<string, object> cDict)
                    {
                        var cmd = new CommandData();
                        if (cDict.ContainsKey("type")) cmd.type = cDict["type"] as string;
                        if (cDict.ContainsKey("id")) cmd.id = cDict["id"] as string;
                        if (cDict.ContainsKey("primitive")) cmd.primitive = cDict["primitive"] as string;
                        if (cDict.ContainsKey("name")) cmd.name = cDict["name"] as string;
                        if (cDict.ContainsKey("target")) cmd.target = cDict["target"] as string;
                        if (cDict.ContainsKey("componentType")) cmd.componentType = cDict["componentType"] as string;
                        if (cDict.ContainsKey("scriptClass")) cmd.scriptClass = cDict["scriptClass"] as string;
                        if (cDict.ContainsKey("all")) cmd.all = (bool)cDict["all"];

                        if (cDict.ContainsKey("position")) cmd.position = ParseVec3(cDict["position"]);
                        if (cDict.ContainsKey("rotation")) cmd.rotation = ParseQuat(cDict["rotation"]);
                        if (cDict.ContainsKey("scale")) cmd.scale = ParseVec3(cDict["scale"]);

                        req.commands.Add(cmd);
                    }
                }
            }
            return req;
        }

        private static Vector3? ParseVec3(object obj)
        {
            var list = obj as List<object>;
            if (list == null || list.Count < 3) return null;
            return new Vector3(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]));
        }

        private static Quaternion? ParseQuat(object obj)
        {
            var list = obj as List<object>;
            if (list == null || list.Count < 4) return null;
            return new Quaternion(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]), ToFloat(list[3]));
        }

        private static float ToFloat(object o)
        {
            return Convert.ToSingle(o, CultureInfo.InvariantCulture);
        }

        // --- Minimal Parser Implementation ---
        
        public static object MiniParse(string json)
        {
            int index = 0;
            return ParseValue(json, ref index);
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '"') return ParseString(json, ref index);
            if (char.IsDigit(c) || c == '-' || c == '.') return ParseNumber(json, ref index);
            if (json.Substring(index).StartsWith("true")) { index += 4; return true; }
            if (json.Substring(index).StartsWith("false")) { index += 5; return false; }
            if (json.Substring(index).StartsWith("null")) { index += 4; return null; }

            return null; // Error or unsupported
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            var dict = new Dictionary<string, object>();
            index++; // skip '{'

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (json[index] == '}') { index++; break; }

                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);
                if (json[index] == ':') index++;

                object value = ParseValue(json, ref index);
                dict[key] = value;

                SkipWhitespace(json, ref index);
                if (json[index] == ',') index++;
            }
            return dict;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            var list = new List<object>();
            index++; // skip '['

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (json[index] == ']') { index++; break; }

                list.Add(ParseValue(json, ref index));

                SkipWhitespace(json, ref index);
                if (json[index] == ',') index++;
            }
            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            var sb = new System.Text.StringBuilder();
            index++; // skip "

            while (index < json.Length)
            {
                if (json[index] == '"' && json[index-1] != '\\') { index++; break; }
                sb.Append(json[index]);
                index++;
            }
            return sb.ToString().Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == '-' || json[index] == 'e' || json[index] == 'E'))
            {
                index++;
            }
            string numStr = json.Substring(start, index - start);
            if (float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
            return 0f;
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
        }
    }
#endif
}
