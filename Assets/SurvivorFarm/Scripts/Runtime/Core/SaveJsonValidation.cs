using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace SurvivorFarm.Runtime.Core
{
    public static class SaveJsonValidation
    {
        // JsonUtility supplies Unity's field serialization, but accepts missing fields
        // and some malformed values. Validate its input using the framework JSON reader.
        public static bool TryValidate(string json, Type schema, out string error)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) throw new FormatException("Empty JSON.");
                byte[] bytes = new UTF8Encoding(false, true).GetBytes(json);
                if (bytes.Length > SaveFileStore.MaximumBytes) throw new FormatException("Save exceeds the size limit.");
                CheckCompleteDocument(json);
                var quotas = new XmlDictionaryReaderQuotas
                {
                    MaxDepth = 64, MaxArrayLength = SaveFileStore.MaximumBytes,
                    MaxStringContentLength = SaveFileStore.MaximumBytes,
                    MaxNameTableCharCount = SaveFileStore.MaximumBytes
                };
                var document = new XmlDocument { XmlResolver = null };
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, quotas)) document.Load(reader);
                XmlElement root = document.DocumentElement;
                Require(root, "object", "root");
                ValidateNode(root, schema, "root", false);
                Require(root["version"], "number", "version");
                Require(root["playerPosition"], "object", "playerPosition");
                Require(root["inventory"], "object", "inventory");
                Require(root["survival"], "object", "survival");
                foreach (string axis in new[] { "x", "y", "z" })
                    Require(root["playerPosition"][axis], "number", "playerPosition." + axis);
                Require(root["survival"]["health"], "number", "survival.health");
                Require(root["survival"]["hungerPercent"], "number", "survival.hungerPercent");
                foreach (string resource in new[] { "wood", "stone", "coins" })
                    Require(root["inventory"][resource], "number", "inventory." + resource);
                if (root["version"].InnerText == "20")
                {
                    // These sections are always written by v20. Their absence must
                    // not silently clear crops, resources, quests or progression.
                    foreach (string section in new[] { "toolUpgrades", "crafting", "tutorialQuest" }) Require(root[section], "object", section);
                    foreach (string section in new[] { "plots", "resources", "resourceSpawns", "unlockZones", "dungeonChests", "groundLoot" }) Require(root[section], "array", section);
                    foreach (string section in new[] { "house", "adventure", "valley", "buildings" })
                        if (root[section] == null) throw new FormatException("Missing " + section + ".");
                    Require(root["day"], "number", "day");
                    Require(root["hour"], "number", "hour");
                }
                error = null;
                return true;
            }
            catch (Exception exception) when (exception is XmlException || exception is FormatException ||
                exception is ArgumentException || exception is InvalidOperationException)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void Require(XmlElement node, string kind, string path)
        {
            if (node == null || node.GetAttribute("type") != kind)
                throw new FormatException("Missing or invalid " + path + ".");
        }

        private static void CheckCompleteDocument(string json)
        {
            // The framework reader can synthesize closing elements at EOF. Check
            // framing first so a truncated file cannot pass as a complete save.
            var closing = new Stack<char>();
            bool quoted = false, escaped = false, finished = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (quoted)
                {
                    if (c < ' ') throw new FormatException("Control character in a JSON string.");
                    if (escaped) { escaped = false; continue; }
                    if (c == '\\') escaped = true;
                    else if (c == '"') quoted = false;
                    continue;
                }
                if (c == ' ' || c == '\r' || c == '\n' || c == '\t') continue;
                if (finished) throw new FormatException("Trailing content after JSON document.");
                if (closing.Count == 0 && c != '{') throw new FormatException("Expected a JSON object.");
                if (c == '"') quoted = true;
                else if (c == '{') closing.Push('}');
                else if (c == '[') closing.Push(']');
                else if (c == '}' || c == ']')
                {
                    if (closing.Count == 0 || closing.Pop() != c) throw new FormatException("Mismatched JSON containers.");
                    finished = closing.Count == 0;
                }
                if (closing.Count > 64) throw new FormatException("JSON nesting exceeds the depth limit.");
            }
            if (!finished || quoted || closing.Count != 0) throw new FormatException("Incomplete JSON document.");
        }

        private static void ValidateNode(XmlElement node, Type schema, string path, bool arrayItem)
        {
            string kind = node.GetAttribute("type");
            if (kind == "null")
            {
                if (schema != null && (schema.IsValueType || arrayItem && schema != typeof(string)))
                    throw new FormatException("Null value at " + path + ".");
                return;
            }
            if (schema == typeof(int))
            {
                Require(node, "number", path);
                if (!int.TryParse(node.InnerText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
                    throw new FormatException("Invalid integer at " + path + ".");
            }
            else if (schema == typeof(float))
            {
                Require(node, "number", path);
                if (!float.TryParse(node.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ||
                    float.IsNaN(number) || float.IsInfinity(number))
                    throw new FormatException("Invalid finite number at " + path + ".");
            }
            else if (schema == typeof(bool)) Require(node, "boolean", path);
            else if (schema == typeof(string)) Require(node, "string", path);
            else if (schema != null)
                Require(node, schema.IsArray || schema.IsGenericType && schema.GetGenericTypeDefinition() == typeof(List<>) ? "array" : "object", path);

            if (kind == "number" && (!double.TryParse(node.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
                double.IsInfinity(value) || double.IsNaN(value)))
                throw new FormatException("Invalid number at " + path + ".");
            if (kind == "boolean" && node.InnerText != "true" && node.InnerText != "false")
                throw new FormatException("Invalid boolean at " + path + ".");

            var names = kind == "object" ? new HashSet<string>(StringComparer.Ordinal) : null;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (!(child is XmlElement element)) continue;
                string name = element.HasAttribute("item") ? element.GetAttribute("item") : element.LocalName;
                if (names != null && !names.Add(name)) throw new FormatException("Duplicate field at " + path + "." + name);
                Type childType = null;
                if (schema != null && kind == "object")
                    childType = schema.GetField(name, BindingFlags.Public | BindingFlags.Instance)?.FieldType;
                else if (schema != null && kind == "array")
                    childType = schema.IsArray ? schema.GetElementType() : schema.GetGenericArguments()[0];
                ValidateNode(element, childType, path + "." + name, kind == "array");
            }
        }
    }
}
