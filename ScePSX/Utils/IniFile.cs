using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ScePSX
{
    public class IniFile
    {
        private readonly string path;
        private readonly Dictionary<string, Dictionary<string, string>> data;

        public IniFile(string inipath)
        {
            path = inipath;
            data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Load();
        }

        private void Load()
        {
            if (!File.Exists(path))
                return;

            string currentSection = null;
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                    if (!data.ContainsKey(currentSection))
                    {
                        data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                } else if (currentSection != null)
                {
                    var parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        data[currentSection][key] = value;
                    }
                }
            }
        }

        private void Save()
        {
            var lines = new List<string>();
            foreach (var section in data)
            {
                lines.Add($"[{section.Key}]");
                foreach (var entry in section.Value)
                {
                    lines.Add($"{entry.Key}={entry.Value}");
                }
                lines.Add(""); // 空行分隔节
            }
            File.WriteAllLines(path, lines);
        }

        public void Write(string section, string key, string value)
        {
            if (!data.ContainsKey(section))
            {
                data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            data[section][key] = value;
            Save();
        }

        public string Read(string section, string key)
        {
            if (data.ContainsKey(section) && data[section].ContainsKey(key))
            {
                return data[section][key];
            }
            return "";
        }

        public void WriteInt(string section, string key, int value)
        {
            Write(section, key, value.ToString());
        }

        public int ReadInt(string section, string key)
        {
            var str = Read(section, key);
            return string.IsNullOrEmpty(str) ? 0 : Convert.ToInt32(str);
        }
    }
}
