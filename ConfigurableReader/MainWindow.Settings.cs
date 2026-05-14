using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Media;

namespace ConfigurableReader;

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

        this.Background = new SolidColorBrush(BackgroundColorPicker.Color);
        MainTextBlock.Foreground = new SolidColorBrush(TextColorPicker.Color);
    }

    private void LoadBookPositionConfiguration()
    {
        try
        {
            if (!string.IsNullOrEmpty(_settings.BookPositionsJson))
            {
                _bookRecords = JsonSerializer.Deserialize<List<BookRecord>>(_settings.BookPositionsJson) ?? [];
            }
        }
        catch
        {
            _bookRecords = [];
        }
    }

    private void SaveSettings()
    {
        _settings.FontSize = (int)(FontSizeNumeric.Value ?? 48);
        _settings.TextColor = TextColorPicker.Color.ToString();
        _settings.BackgroundColor = BackgroundColorPicker.Color.ToString();
        _settings.ScrollSpeed = SpeedSlider.Value;
        _settings.EnableEdgeFading = FadeCheckBox.IsChecked ?? true;

        if (FontComboBox.SelectedItem is FontFamily fontFamily)
        {
            _settings.FontFamily = fontFamily.Name;
        }

        if (_currentBookFileName != null)
        {
            string bookName = Path.GetFileName(_currentBookFileName);
            var record = _bookRecords.FirstOrDefault(r => r.Name == bookName);
            if (record == null)
            {
                record = new BookRecord { Name = bookName };
                _bookRecords.Add(record);
            }
            record.ScrollPosition = _readerService.CurrentPosition;
            _settings.BookPositionsJson = JsonSerializer.Serialize(_bookRecords);
        }

        _settings.Save();
    }
}
