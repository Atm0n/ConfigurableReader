using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using ConfigurableReader.Common;
using ConfigurableReader.Services;
using System.Linq;

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
        UpdateFontSize(_settings.FontSize);

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
        UpdateTextColor(new SolidColorBrush(TextColorPicker.Color));

        // Apply Theme
        using (_controller.SuppressCodeUpdates())
        {
            ThemeComboBox.SelectedItem = ThemeComboBox.Items
            .Cast<ComboBoxItem>()
            .FirstOrDefault(i => i.Tag?.ToString() == _settings.Theme) ?? ThemeComboBox.Items.Cast<ComboBoxItem>().First();

            SpeedReadingCheckBox.IsChecked = _settings.SpeedReadingMode;
            SpeedReadingBoldSlider.Value = _settings.SpeedReadingBoldRatio * 100;
            SpeedReadingBoldValueText.Text = $"{(int)(_settings.SpeedReadingBoldRatio * 100)}%";
            SpeedReadingBoldPanel.IsVisible = _settings.SpeedReadingMode;

            ApplyThemeColor(_settings.Theme);

            // Reading Mode & RSVP
            ReadingModeComboBox.SelectedItem = ReadingModeComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == _settings.ReadingMode) ?? ReadingModeComboBox.Items.Cast<ComboBoxItem>().First();

            RsvpWpmNumeric.Value = _settings.RsvpWpm;
            ApplyReadingMode(_settings.ReadingMode);
        }
    }

    public void ApplyReadingMode(string mode)
    {
        bool isRsvp = mode == "RSVP";
        _settings.ReadingMode = isRsvp ? "RSVP" : "Marquee";

        ReadingAreaCanvas.IsVisible = !isRsvp;
        RsvpAreaContainer.IsVisible = isRsvp;
        MarqueeSpeedPanel.IsVisible = !isRsvp;
        RsvpSpeedPanel.IsVisible = isRsvp;

        _renderedBasePosition = -1;
        _currentRsvpWord = null;
        _rsvpRemainingDelayMs = 0;

        UpdateDisplayedText();
        if (!isRsvp)
        {
            UpdateRenderTransform();
        }
        UpdateReadingStats();
    }

    public void ToggleReadingMode()
    {
        string nextMode = _settings.ReadingMode == "RSVP" ? "Marquee" : "RSVP";
        _settings.ReadingMode = nextMode;
        using (_controller.SuppressCodeUpdates())
        {
            ReadingModeComboBox.SelectedItem = ReadingModeComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == nextMode);
        }
        ApplyReadingMode(nextMode);
        _settings.Save();
    }

    public void CycleNextTheme()
    {
        var selectablePresets = ReaderTheme.Presets.Where(p => p.Key != "Custom").ToList();
        string currentKey = _settings.Theme;
        int currentIndex = selectablePresets.FindIndex(p => p.Key == currentKey);
        int nextIndex = (currentIndex + 1) % selectablePresets.Count;
        var nextPreset = selectablePresets[nextIndex];

        _settings.Theme = nextPreset.Key;
        using (_controller.SuppressCodeUpdates())
        {
            ThemeComboBox.SelectedItem = ThemeComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == nextPreset.Key);
        }
        ApplyThemeColor(nextPreset.Key);
        _settings.Save();
    }

    private void ApplyThemeColor(string themeName)
    {
        string effectiveTheme = themeName;
        if (effectiveTheme == "System Default")
        {
            var isDark = Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark;
            effectiveTheme = isDark ? "Dark" : "Light";
        }

        var preset = ReaderTheme.Presets.FirstOrDefault(p => p.Key == effectiveTheme);
        if (preset != null && effectiveTheme != "Custom")
        {
            SetThemeColors(preset);
        }
        else if (effectiveTheme == "Custom")
        {
            if (Color.TryParse(_settings.BackgroundColor, out var bg))
                this.Background = new SolidColorBrush(bg);
            if (Color.TryParse(_settings.TextColor, out var fg))
                UpdateTextColor(new SolidColorBrush(fg));
        }

        CustomColorPanel.IsVisible = (themeName == "Custom" || _settings.Theme == "Custom");
    }

    private void SetThemeColors(ReaderTheme theme)
    {
        var bgColor = Color.Parse(theme.Background);
        var fgColor = Color.Parse(theme.Foreground);
        var barBg = Color.Parse(theme.ControlBarBackground);
        var barFg = Color.Parse(theme.ControlBarForeground);
        var borderColor = Color.Parse(theme.BorderBrush);

        using (_controller.SuppressCodeUpdates())
        {
            BackgroundColorPicker.Color = bgColor;
            TextColorPicker.Color = fgColor;
        }

        this.Background = new SolidColorBrush(bgColor);
        UpdateTextColor(new SolidColorBrush(fgColor));

        if (BottomControlBar != null)
        {
            BottomControlBar.Background = new SolidColorBrush(barBg);
            BottomControlBar.BorderBrush = new SolidColorBrush(borderColor);
        }

        if (BookNameText != null) BookNameText.Foreground = new SolidColorBrush(barFg);
        if (PercentageText != null) PercentageText.Foreground = new SolidColorBrush(barFg);
        if (ReadingStatsText != null) ReadingStatsText.Foreground = new SolidColorBrush(barFg);
    }

    private void UpdateTextColor(IBrush brush)
    {
        MainTextBlock.Foreground = brush;
        if (RsvpPrefixText != null) RsvpPrefixText.Foreground = brush;
        if (RsvpSuffixText != null) RsvpSuffixText.Foreground = brush;
    }

    private void UpdateFontSize(double size)
    {
        MainTextBlock.FontSize = size;
        if (RsvpPrefixText != null) RsvpPrefixText.FontSize = size;
        if (RsvpOrpText != null) RsvpOrpText.FontSize = size;
        if (RsvpSuffixText != null) RsvpSuffixText.FontSize = size;
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
        _settings.SpeedReadingBoldRatio = SpeedReadingBoldSlider.Value / 100.0;

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

        if (ReadingModeComboBox.SelectedItem is ComboBoxItem modeItem && modeItem.Tag != null)
        {
            _settings.ReadingMode = modeItem.Tag.ToString() ?? "Marquee";
        }
        _settings.RsvpWpm = (int)(RsvpWpmNumeric.Value ?? 300);

        _controller.SaveCurrentPosition();

        _settings.Save();
    }

    private void ReadingModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode) return;
        if (ReadingModeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string mode = item.Tag.ToString() ?? "Marquee";
            ApplyReadingMode(mode);
            _settings.Save();
        }
    }

    private void RsvpWpmNumeric_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode || !e.NewValue.HasValue) return;
        _settings.RsvpWpm = (int)e.NewValue.Value;
        _settings.Save();
        UpdateReadingStats();
    }

    private void FontComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FontComboBox.SelectedItem is FontFamily fontFamily)
        {
            MainTextBlock.FontFamily = fontFamily;
            if (RsvpPrefixText != null) RsvpPrefixText.FontFamily = fontFamily;
            if (RsvpOrpText != null) RsvpOrpText.FontFamily = fontFamily;
            if (RsvpSuffixText != null) RsvpSuffixText.FontFamily = fontFamily;
        }
    }

    private void LanguageComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingFromCode && LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string langCode = item.Tag.ToString() ?? "en-US";
            LocalizationService.SetLanguage(langCode);
        }
    }

    private void FadeCheckBox_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode) return;
        bool enable = FadeCheckBox.IsChecked ?? true;
        _settings.EnableEdgeFading = enable;
        _settings.Save();
        UpdateEdgeFading(enable);
    }

    private void SpeedReadingCheckBox_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode) return;
        _settings.SpeedReadingMode = SpeedReadingCheckBox.IsChecked ?? false;
        SpeedReadingBoldPanel.IsVisible = _settings.SpeedReadingMode;
        _settings.Save();

        // Force a layout refresh for the current text
        _renderedBasePosition = -1; // Reset to force complete text refresh
        UpdateDisplayedText();
    }

    private void SpeedReadingBoldSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode || _settings == null || SpeedReadingBoldSlider == null || SpeedReadingBoldValueText == null) return;

        int percent = (int)SpeedReadingBoldSlider.Value;
        SpeedReadingBoldValueText.Text = $"{percent}%";

        _settings.SpeedReadingBoldRatio = percent / 100.0;
        _settings.Save();

        _renderedBasePosition = -1; // Reset to force complete text refresh
        UpdateDisplayedText();
    }

    private void TextColorPicker_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode) return;
        MainTextBlock.Foreground = new SolidColorBrush(e.NewColor);
        SetThemeToCustom();
    }

    private void BackgroundColorPicker_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode) return;
        this.Background = new SolidColorBrush(e.NewColor);
        SetThemeToCustom();
    }

    private void SetThemeToCustom()
    {
        _settings.Theme = "Custom";
        using (_controller.SuppressCodeUpdates())
        {
            ThemeComboBox.SelectedItem = ThemeComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == "Custom");
            CustomColorPanel.IsVisible = true;
        }
        _settings.Save();
    }
}
