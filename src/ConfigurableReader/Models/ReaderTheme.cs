using System.Collections.Generic;

namespace ConfigurableReader.Models;

public record ReaderTheme(
    string Key,
    string LocalizationKey,
    string Background,
    string Foreground,
    string ControlBarBackground,
    string ControlBarForeground,
    string BorderBrush
)
{
    public static readonly IReadOnlyList<ReaderTheme> Presets =
    [
        new("System Default", "ThemeSystemDefault", "#1E1E1E", "#F1F1F1", "#2D2D30", "#F1F1F1", "#3E3E42"),
        new("Dark", "ThemeDark", "#1E1E1E", "#F1F1F1", "#2D2D30", "#F1F1F1", "#3E3E42"),
        new("Light", "ThemeLight", "#FAFAFA", "#1A1A1A", "#EDEDED", "#1A1A1A", "#D0D0D0"),
        new("Sepia", "ThemeSepia", "#FBF0D9", "#4A3525", "#EFE3C6", "#4A3525", "#DDD0B2"),
        new("OLED Black", "ThemeOled", "#000000", "#E6E6E6", "#0F0F0F", "#E6E6E6", "#252525"),
        new("Solarized Dark", "ThemeSolarized", "#002B36", "#93A1A1", "#073642", "#93A1A1", "#586E75"),
        new("Nord", "ThemeNord", "#2E3440", "#ECEFF4", "#3B4252", "#ECEFF4", "#4C566A"),
        new("High Contrast", "ThemeHighContrast", "#000000", "#00FF00", "#000000", "#00FF00", "#00FF00"),
        new("Custom", "ThemeCustom", "#1E1E1E", "#F1F1F1", "#2D2D30", "#F1F1F1", "#3E3E42")
    ];
}
