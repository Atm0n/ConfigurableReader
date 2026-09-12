using System;
using System.IO;
using System.Text.Json;

namespace ConfigurableReader.Models;

public class AppSettings
{
    public double FontSize { get; set; } = 48;
    public string TextColor { get; set; } = "#F1F1F1";
    public string BackgroundColor { get; set; } = "#1E1E1E";
    public string Language { get; set; } = Services.LocalizationService.GetSystemLanguage();
    public double ScrollSpeed { get; set; } = 100;
    public string? FontFamily { get; set; }
    public bool EnableEdgeFading { get; set; } = true;
    public bool SpeedReadingMode { get; set; } = false;
    public double SpeedReadingBoldRatio { get; set; } = 0.5;
    public string Theme { get; set; } = "System Default";
    public string LibrarySortOption { get; set; } = "Recent";
    public string ReadingMode { get; set; } = "Marquee";
    public int RsvpWpm { get; set; } = 300;
    public KeyBindingsConfig KeyBindings { get; set; } = KeyBindingsConfig.GetDefaultBindings();

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
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                settings.KeyBindings ??= KeyBindingsConfig.GetDefaultBindings();
                return settings;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string? directory = Path.GetDirectoryName(SettingsPath);
            if (directory != null) Directory.CreateDirectory(directory);
            string tempPath = SettingsPath + ".tmp";
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, SettingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
