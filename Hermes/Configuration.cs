using System.Text.Json;
using Hermes.Utilities;

namespace Hermes;

public class Configuration
{
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 2053;
    public string ServerName  { get; set; } = "localhost";
    public string Environment { get; set; } = "Development";
    public int HttpPort { get; set; } = 8080;
    
    private static string ConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    
    public static Configuration Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<Configuration>(json) ?? new Configuration();
            }
                
            var config = new Configuration();
            Save(config);
            return config;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading configuration: {ex.Message}");
            return new Configuration();
        }
    }

         
    public static void Save(Configuration config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error saving configuration: {ex.Message}");
        }
    }
}