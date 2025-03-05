using System;
using System.Collections.Generic;
using System.IO;

namespace ScePSX
{
    static class SimpleYaml
    {
        static Dictionary<string, object> yamlData = new Dictionary<string, object>();

        public static void ParseYamlFile(string filePath)
        {
            var stack = new Stack<(int indent, Dictionary<string, object> dict)>();
            stack.Push((0, yamlData));

            string[] lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue; // 忽略空行和注释

                int indentLevel = line.Length - trimmedLine.Length;

                // 调整栈以匹配当前缩进
                while (stack.Count > 1 && stack.Peek().indent >= indentLevel)
                {
                    stack.Pop();
                }

                if (trimmedLine.Contains(":") && !trimmedLine.Contains(": "))
                {
                    // 处理键（无值）
                    string key = trimmedLine.Replace(":", "").Trim();
                    var newDict = new Dictionary<string, object>();
                    stack.Peek().dict[key] = newDict;
                    stack.Push((indentLevel, newDict));
                } else if (trimmedLine.Contains(": "))
                {
                    // 处理键值对
                    int separatorIndex = trimmedLine.IndexOf(": ");
                    string key = trimmedLine.Substring(0, separatorIndex).Trim();
                    string value = trimmedLine.Substring(separatorIndex + 2).Trim();

                    if (value == "true" || value == "false")
                    {
                        stack.Peek().dict[key] = bool.Parse(value);
                    } else if (int.TryParse(value, out int intValue))
                    {
                        stack.Peek().dict[key] = intValue;
                    } else
                    {
                        stack.Peek().dict[key] = value;
                    }
                } else if (trimmedLine.StartsWith("- "))
                {
                    // 处理列表项
                    string listItem = trimmedLine.Substring(2).Trim();
                    if (stack.Peek().dict.Count > 0)
                    {
                        string lastKey = null;
                        foreach (var kvp in stack.Peek().dict)
                        {
                            lastKey = kvp.Key;
                        }

                        if (lastKey != null && stack.Peek().dict[lastKey] is List<object> list)
                        {
                            list.Add(listItem);
                        } else
                        {
                            stack.Peek().dict[lastKey] = new List<object> { listItem };
                        }
                    }
                }
            }
        }

        public static string TryGetValue(string keyPath)
        {
            string[] keys = keyPath.Split('.');
            object current = yamlData;

            foreach (var key in keys)
            {
                if (current is Dictionary<string, object> dict && dict.ContainsKey(key))
                {
                    current = dict[key];
                } else
                {
                    return "";
                }
            }

            return current.ToString();
        }
    }
}
