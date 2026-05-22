using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using ConfigurableReader.Common;
using ConfigurableReader.Services;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;

namespace ConfigurableReader.Views;

using ConfigurableReader.Models;

public partial class MainWindow
{
    private AppSettings _settings = new();

    private void PopulateFontList()
    {
        var fonts = FontManager.Current.SystemFonts.OrderBy(f => f.Name).ToList();
        FontComboBox.ItemsSource = fonts;

        if (_settings.FontFamily != null)
        {
            FontComboBox.SelectedItem = fonts.FirstOrDefault(f => f.Name == _settings.FontFamily);
        }
    }

    private void ApplySettings()
    {
        FontSizeNumeric.Value = (decimal)_settings.FontSize;
        MainTextBlock.FontSize = _settings.FontSize;

        if (Color.TryParse(_settings.TextColor, out var textColor))
            TextColorPicker.Color = textColor;

        if (Color.TryParse(_settings.BackgroundColor, out var bgColor))
            BackgroundColorPicker.Color = bgColor;

        SpeedSlider.Value = _settings.ScrollSpeed;
        FadeCheckBox.IsChecked = _settings.EnableEdgeFading;
        UpdateEdgeFading(_settings.EnableEdgeFading);

        // Populate language combo from AppConstants — single source of truth
        using (_controller.SuppressCodeUpdates())
        {
            LanguageComboBox.Items.Clear();
            foreach (var (code, displayName) in AppConstants.SupportedLanguages)
            {
                LanguageComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = code });
            }

            var langItem = LanguageComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag?.ToString() == _settings.Language);
            if (langItem != null)
            {
                LanguageComboBox.SelectedItem = langItem;
                LocalizationService.SetLanguage(_settings.Language);
            }
        }

        this.Background = new SolidColorBrush(BackgroundColorPicker.Color);
        MainTextBlock.Foreground = new SolidColorBrush(TextColorPicker.Color);

        // Apply Theme
        using (_controller.SuppressCodeUpdates())
        {
            ThemeComboBox.SelectedItem = ThemeComboBox.Items
            .Cast<ComboBoxItem>()
            .FirstOrDefault(i => i.Tag?.ToString() == _settings.Theme) ?? ThemeComboBox.Items.Cast<ComboBoxItem>().First();
        
            SpeedReadingCheckBox.IsChecked = _settings.SpeedReadingMode;

            ApplyThemeColor(_settings.Theme);
        }
    }

    private void ApplyThemeColor(string themeName)
    {
        if (themeName == "System Default")
        {
            var isDark = Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark;
            themeName = isDark ? "Dark" : "Light";
        }

        switch (themeName)
        {
            case "Dark":
                SetColors(Color.Parse("#1E1E1E"), Color.Parse("#F1F1F1"));
                break;
            case "Light":
                SetColors(Color.Parse("#FAFAFA"), Color.Parse("#1A1A1A"));
                break;
            case "Sepia":
                SetColors(Color.Parse("#F4ECD8"), Color.Parse("#5B4636"));
                break;
            case "High Contrast":
                SetColors(Color.Parse("#000000"), Color.Parse("#00FF00"));
                break;
            case "Custom":
                // Don't change colors, just use the custom ones.
                break;
        }

        CustomColorPanel.IsVisible = (themeName == "Custom" || _settings.Theme == "Custom");
    }

    private void SetColors(Color bgColor, Color fgColor)
    {
        using (_controller.SuppressCodeUpdates())
        {
            BackgroundColorPicker.Color = bgColor;
            TextColorPicker.Color = fgColor;
        }
        this.Background = new SolidColorBrush(bgColor);
        MainTextBlock.Foreground = new SolidColorBrush(fgColor);
    }

    private void ThemeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode) return;
        
        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            _settings.Theme = item.Tag.ToString() ?? "System Default";
            ApplyThemeColor(_settings.Theme);
            _settings.Save();
        }
    }

    private void LoadBookPositionConfiguration()
    {
        _controller.LoadBookRecords();
    }

    private void SaveSettings()
    {
        _settings.FontSize = (double)(FontSizeNumeric.Value ?? 48);
        _settings.TextColor = TextColorPicker.Color.ToString();
        _settings.BackgroundColor = BackgroundColorPicker.Color.ToString();
        _settings.ScrollSpeed = SpeedSlider.Value;
        _settings.EnableEdgeFading = FadeCheckBox.IsChecked ?? true;

        if (LanguageComboBox.SelectedItem is ComboBoxItem langItem && langItem.Tag != null)
        {
            _settings.Language = langItem.Tag.ToString() ?? "en-US";
        }

        if (FontComboBox.SelectedItem is FontFamily fontFamily)
        {
            _settings.FontFamily = fontFamily.Name;
        }

        if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem && themeItem.Tag != null)
        {
            _settings.Theme = themeItem.Tag.ToString() ?? "System Default";
        }

        _controller.SaveCurrentPosition();

        _settings.Save();
    }
}
