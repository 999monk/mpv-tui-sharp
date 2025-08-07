using System.Text.Json;
using MpvTuiSharp.Models;

namespace MpvTuiSharp.Services;

public class ConfigurationService
{
    private readonly string _configPath;
    private readonly string _configFileName = "config.json";

    public ConfigurationService()
    {
        var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cli-mpv");
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }
        _configPath = Path.Combine(configDir, _configFileName);
    }

    public Config? LoadConfig()
    {
        if (!File.Exists(_configPath)) return null;
        
        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<Config>(json);
    }

    public void SaveConfig(Config config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
