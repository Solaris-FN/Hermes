using System.Text.Json;

namespace Hermes;

public class Configuration
{
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 5222;
    
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
            Console.WriteLine($"Error loading configuration: {ex.Message}");
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
            Console.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }
}