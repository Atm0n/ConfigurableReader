using System;
using System.IO;
using System.Text.Json;

namespace ConfigurableReader;

public class AppSettings
{
    public int FontSize { get; set; } = 48;
    public string TextColor { get; set; } = "#F1F1F1";
    public string BackgroundColor { get; set; } = "#1E1E1E";
    public string Language { get; set; } = Services.LocalizationService.GetSystemLanguage();
    public double ScrollSpeed { get; set; } = 100;
    public string? FontFamily { get; set; }
    public bool EnableEdgeFading { get; set; } = true;
    public string BookPositionsJson { get; set; } = "[]";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ConfigurableReader",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string? directory = Path.GetDirectoryName(SettingsPath);
            if (directory != null) Directory.CreateDirectory(directory);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
