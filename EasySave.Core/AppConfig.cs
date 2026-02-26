using System;
using System.IO;
using System.Text.Json;

namespace EasySave.Core
{
    public class RemoteLoggingConfig
    {
        public bool Enabled { get; set; }
        public string ServerHost { get; set; } = "localhost";
        public int ServerPort { get; set; } = 5555;
        public bool EnableLocalLogging { get; set; } = true;
    }

    public class LoggingConfig
    {
        public string LogDirectory { get; set; } = "./logs";
        public string LogFormat { get; set; } = "Json";
    }

    public class EncryptionConfig
    {
        public bool Enabled { get; set; } = false;
        public string Key { get; set; } = string.Empty;
        public List<string> Extensions { get; set; } = new();
    }

    public class AppConfig
    {
        public RemoteLoggingConfig RemoteLogging { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
        public EncryptionConfig Encryption { get; set; } = new();

        private static AppConfig? _instance;
        private static readonly object _lock = new();

        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = LoadConfig();
                        }
                    }
                }
                return _instance;
            }
        }

        private static AppConfig LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                if (!File.Exists(configPath))
                {
                    // Return default config
                    return new AppConfig();
                }

                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return config ?? new AppConfig();
            }
            catch (Exception)
            {
                // If config loading fails, return default
                return new AppConfig();
            }
        }

        public void SaveConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        public static void Reload()
        {
            lock (_lock)
            {
                _instance = LoadConfig();
            }
        }
    }
}
