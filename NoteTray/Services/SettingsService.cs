using System.IO;
using System.Text.Json;
using NoteTray.Models;

namespace NoteTray.Services;

public class SettingsService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteTray");
    private static readonly string SettingsPath = Path.Combine(AppDataPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings Settings { get; private set; } = new();

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(AppDataPath);
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
