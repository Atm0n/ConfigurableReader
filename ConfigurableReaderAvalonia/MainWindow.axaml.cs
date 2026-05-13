using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Controls.Primitives;

namespace ConfigurableReaderAvalonia;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly TranslateTransform _textTranslateTransform = new();
    private int _currentPosition = 0;
    private bool IsPaused = true;
    private string? _currentBookFileName;
    private string _fullText = string.Empty;
    
    private List<BookRecord> _bookRecords = [];
    private AppSettings _settings = new();
    
    private bool isReversing = false;
    private bool _isUpdatingFromCode = false;

    private double _currentOffsetX = 0;
    private DateTime _lastRenderTime;
    private readonly Dictionary<char, double> _charWidths = [];

    private DateTime _lastKeyUpTime = DateTime.MinValue;
    private DateTime _lastKeyDownTime = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20),
        };
        _timer.Tick += (s, e) => {
            OnRendering();
        };

        _settings = AppSettings.Load();
        LoadBookPositionConfiguration();
        PopulateFontList();
        ApplySettings();

        MainTextBlock.RenderTransform = _textTranslateTransform;
        _timer.Start();
    }

    private void OnRendering()
    {
        if (IsPaused || string.IsNullOrEmpty(_fullText)) return;

        DateTime now = DateTime.Now;
        if (_lastRenderTime == DateTime.MinValue)
        {
            _lastRenderTime = now;
            return;
        }

        double deltaTime = (now - _lastRenderTime).TotalSeconds;
        _lastRenderTime = now;

        double speed = SpeedSlider.Value;
        double pixelsToMove = speed * deltaTime;

        if (isReversing)
        {
            _currentOffsetX += pixelsToMove;
            while (_currentOffsetX > 0)
            {
                if (_currentPosition > 0)
                {
                    _currentPosition--;
                    double charWidth = GetCharacterWidth(_fullText[_currentPosition]);
                    _currentOffsetX -= charWidth;
                }
                else
                {
                    _currentOffsetX = 0;
                    StopReading();
                    MessageDialog.Show(this, "Start of the book reached");
                    break;
                }
            }
        }
        else
        {
            _currentOffsetX -= pixelsToMove;
            while (true)
            {
                if (_currentPosition >= _fullText.Length)
                {
                    _currentOffsetX = 0;
                    StopReading();
                    MessageDialog.Show(this, "End of the book reached");
                    break;
                }

                double charWidth = GetCharacterWidth(_fullText[_currentPosition]);
                if (_currentOffsetX <= -charWidth)
                {
                    _currentOffsetX += charWidth;
                    _currentPosition++;
                }
                else
                {
                    break;
                }
            }
        }

        _textTranslateTransform.X = _currentOffsetX;
        
        // Centering logic
        if (ReadingAreaCanvas.Bounds.Height > 0 && MainTextBlock.Bounds.Height > 0)
        {
            double top = (ReadingAreaCanvas.Bounds.Height - MainTextBlock.Bounds.Height) / 2;
            Canvas.SetTop(MainTextBlock, top);
        }
        
        UpdateDisplayedText();
    }

    private void UpdateDisplayedText()
    {
        int length = Math.Min(5000, _fullText.Length - _currentPosition);
        string newText = _fullText.Substring(_currentPosition, length);
        
        if (MainTextBlock.Text != newText)
        {
            MainTextBlock.Text = newText;
        }

        _isUpdatingFromCode = true;
        TextSlider.Value = _currentPosition;
        _isUpdatingFromCode = false;
        
        UpdatePercentage();
    }

    private double GetCharacterWidth(char c)
    {
        if (_charWidths.TryGetValue(c, out double width)) return width;

        var typeface = new Typeface(MainTextBlock.FontFamily, MainTextBlock.FontStyle, MainTextBlock.FontWeight);
        
        var textLayout = new TextLayout(
            c.ToString(),
            typeface,
            MainTextBlock.FontSize,
            MainTextBlock.Foreground);

        width = textLayout.TextLines[0].Width;
        _charWidths[c] = width;
        return width;
    }

    private void ClearCharWidthCache()
    {
        _charWidths.Clear();
    }

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
            record.ScrollPosition = _currentPosition;
            _settings.BookPositionsJson = JsonSerializer.Serialize(_bookRecords);
        }

        _settings.Save();
    }

    private void UpdatePercentage()
    {
        if (_fullText.Length > 0)
        {
            double percentage = (double)_currentPosition / _fullText.Length * 100;
            PercentageText.Text = $"{percentage:F1}%";
        }
    }

    private void UpdateEdgeFading(bool enable)
    {
        ReadingAreaCanvas.OpacityMask = enable ? (IBrush)Resources["EdgeFadeMask"]! : null;
    }

    private void AdjustFontSize(int delta)
    {
        double newSize = MainTextBlock.FontSize + delta;
        MainTextBlock.FontSize = Math.Clamp(newSize, 10, 800);
        FontSizeNumeric.Value = (decimal)MainTextBlock.FontSize;
        ClearCharWidthCache();
    }

    private async void OpenFileButton_Click(object? sender, RoutedEventArgs e)
    {
        var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Open Text File",
            FileTypeFilter = [new Avalonia.Platform.Storage.FilePickerFileType("Text Files") { Patterns = ["*.txt"] }]
        };

        var result = await this.StorageProvider.OpenFilePickerAsync(options);
        if (result.Count > 0)
        {
            SaveSettings();

            _currentBookFileName = result[0].Path.LocalPath;
            string bookName = Path.GetFileName(_currentBookFileName);

            _fullText = File.ReadAllText(_currentBookFileName).Replace("\r", " ").Replace("\n", " ").Replace("  ", " ");

            var actualBook = _bookRecords.FirstOrDefault(r => r.Name == bookName);
            if (actualBook is null)
            {
                actualBook = new BookRecord { Name = bookName };
                _bookRecords.Add(actualBook);
            }

            _isUpdatingFromCode = true;
            _currentPosition = actualBook.ScrollPosition;
            _currentOffsetX = 0;
            _textTranslateTransform.X = 0;

            TextSlider.Maximum = _fullText.Length;
            TextSlider.Value = _currentPosition;
            BookNameText.Text = bookName;
            
            UpdateDisplayedText();
            UpdatePercentage();
            _isUpdatingFromCode = false;

            SaveSettings();
        }
    }

    private void StartStopButton_Click(object? sender, RoutedEventArgs e) => ToggleStartStop();
    private void ReverseButton_Click(object? sender, RoutedEventArgs e) => isReversing = !isReversing;
    private void InfoButton_Click(object? sender, RoutedEventArgs e) => ShowInfo();

    private void ToggleStartStop()
    {
        if (_currentBookFileName == null) return;

        if (IsPaused)
        {
            StartStopButton.Content = "Stop";
            IsPaused = false;
            _lastRenderTime = DateTime.MinValue;
            SettingsExpander.IsExpanded = false;
        }
        else
        {
            StopReading();
        }
    }

    private void StopReading()
    {
        StartStopButton.Content = "Start";
        IsPaused = true;
    }

    private void ShowInfo()
    {
        var dialog = new InfoDialog();
        dialog.ShowDialog(this);
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left: isReversing = true; break;
            case Key.Right: isReversing = false; break;
            case Key.Space: ToggleStartStop(); break;
            case Key.Up:
                int upStep = (DateTime.Now - _lastKeyUpTime).TotalMilliseconds < 400 ? 10 : 1;
                _lastKeyUpTime = DateTime.Now;
                AdjustFontSize(upStep);
                break;
            case Key.Down:
                int downStep = (DateTime.Now - _lastKeyDownTime).TotalMilliseconds < 400 ? -10 : -1;
                _lastKeyDownTime = DateTime.Now;
                AdjustFontSize(downStep);
                break;
            case Key.R: isReversing = !isReversing; break;
            case Key.F: FadeCheckBox.IsChecked = !FadeCheckBox.IsChecked; break;
            case Key.S: SettingsExpander.IsExpanded = !SettingsExpander.IsExpanded; break;
            case Key.I: ShowInfo(); break;
            case Key.OemPlus: case Key.Add: SpeedSlider.Value += 50; break;
            case Key.OemMinus: case Key.Subtract: SpeedSlider.Value -= 50; break;
        }
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        SaveSettings();
    }

    private void FontComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FontComboBox.SelectedItem is FontFamily fontFamily)
        {
            MainTextBlock.FontFamily = fontFamily;
            ClearCharWidthCache();
        }
    }

    private void FadeCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        UpdateEdgeFading(FadeCheckBox.IsChecked ?? true);
    }

    private void TextColorPicker_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        MainTextBlock.Foreground = new SolidColorBrush(e.NewColor);
        ClearCharWidthCache();
    }

    private void BackgroundColorPicker_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        this.Background = new SolidColorBrush(e.NewColor);
    }

    private void TextSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (IsPaused && !_isUpdatingFromCode)
        {
            _currentPosition = (int)TextSlider.Value;
            _currentOffsetX = 0;
            _textTranslateTransform.X = 0;
            UpdateDisplayedText();
        }
    }
}
