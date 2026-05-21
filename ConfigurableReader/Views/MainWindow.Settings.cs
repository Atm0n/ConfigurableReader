using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using ConfigurableReader.Common;
using ConfigurableReader.Services;

namespace ConfigurableReader.Views;

using ConfigurableReader.Models;

public partial class MainWindow
{
    private List<BookRecord> _bookRecords = [];
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
        _isUpdatingFromCode = true;
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
        _isUpdatingFromCode = false;

        this.Background = new SolidColorBrush(BackgroundColorPicker.Color);
        MainTextBlock.Foreground = new SolidColorBrush(TextColorPicker.Color);
    }

    private void LoadBookPositionConfiguration()
    {
        _bookRecords = BookRecordStore.Load();
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

        if (_currentBookFileName != null)
        {
            var record = _bookRecords.FirstOrDefault(r => r.FilePath == _currentBookFileName);
            if (record == null)
            {
                record = new BookRecord { FilePath = _currentBookFileName };
                _bookRecords.Add(record);
            }
            record.ScrollPosition = _readerService.CurrentPosition;
            BookRecordStore.Save(_bookRecords);
        }

        _settings.Save();
    }
}
