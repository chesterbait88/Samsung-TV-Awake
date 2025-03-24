using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TVMonitorApp.Utils
{
    public class ConfigManager
    {
        public string ConfigFilePath { get; private set; }
        private Dictionary<string, Dictionary<string, string>> configSections;

        public ConfigManager()
        {
            string appPath = AppDomain.CurrentDomain.GetData("APP_PATH") as string ?? AppDomain.CurrentDomain.BaseDirectory;
            ConfigFilePath = Path.Combine(appPath, "config.ini");
            configSections = new Dictionary<string, Dictionary<string, string>>();
        }

        public bool LoadConfiguration()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return false;
            }

            configSections.Clear();
            string currentSection = "";

            try
            {
                foreach (string line in File.ReadAllLines(ConfigFilePath))
                {
                    string trimmedLine = line.Trim();

                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    {
                        continue; // Skip empty lines and comments
                    }

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        if (!configSections.ContainsKey(currentSection))
                        {
                            configSections[currentSection] = new Dictionary<string, string>();
                        }
                    }
                    else if (trimmedLine.Contains("="))
                    {
                        int index = trimmedLine.IndexOf('=');
                        string key = trimmedLine.Substring(0, index).Trim();
                        string value = trimmedLine.Substring(index + 1).Trim();

                        if (!string.IsNullOrEmpty(currentSection) && !string.IsNullOrEmpty(key))
                        {
                            configSections[currentSection][key] = value;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error loading configuration: {ex.Message}", "Configuration Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(ConfigFilePath, false, Encoding.UTF8))
                {
                    foreach (var section in configSections)
                    {
                        writer.WriteLine($"[{section.Key}]");
                        
                        foreach (var kvp in section.Value)
                        {
                            writer.WriteLine($"{kvp.Key} = {kvp.Value}");
                        }
                        
                        writer.WriteLine(); // Add empty line between sections
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error saving configuration: {ex.Message}", "Configuration Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        public string GetValue(string section, string key, string defaultValue = "")
        {
            if (configSections.ContainsKey(section) && configSections[section].ContainsKey(key))
            {
                return configSections[section][key];
            }
            return defaultValue;
        }

        public void SetValue(string section, string key, string value)
        {
            if (!configSections.ContainsKey(section))
            {
                configSections[section] = new Dictionary<string, string>();
            }
            
            configSections[section][key] = value;
        }

        public bool HasSection(string section)
        {
            return configSections.ContainsKey(section);
        }

        public bool HasValue(string section, string key)
        {
            return configSections.ContainsKey(section) && configSections[section].ContainsKey(key);
        }
    }
}