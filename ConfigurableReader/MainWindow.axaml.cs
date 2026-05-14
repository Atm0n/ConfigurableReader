using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
using ConfigurableReader.Services;
using ConfigurableReader.Core;
using ConfigurableReader.Parsers.Txt;
using ConfigurableReader.Parsers.Epub;
using System.Collections.Generic;

namespace ConfigurableReader;

public partial class MainWindow : Window
{
    private readonly GamepadService _gamepadService = new();
    private readonly ReaderService _readerService = new();
    private readonly DocumentRegistry _documentRegistry = new();
    
    private string? _currentBookFileName;
    private bool _isUpdatingFromCode = false;

    private DateTime _lastKeyUpTime = DateTime.MinValue;
    private DateTime _lastKeyDownTime = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(AppConstants.TimerIntervalMs),
        };

        _settings = AppSettings.Load();
        LoadBookPositionConfiguration();
        PopulateFontList();
        ApplySettings();

        InitializeParsers();
        InitializeRendering();
        InitializeGamepad();

        _readerService.StartOfBookReached += () => 
        {
            Dispatcher.UIThread.Post(() => _ = OnStartOfBookReachedAsync());
        };

        _readerService.EndOfBookReached += () => 
        {
            Dispatcher.UIThread.Post(() => _ = OnEndOfBookReachedAsync());
        };
    }

    private void InitializeParsers()
    {
        _documentRegistry.RegisterParser(new TxtBookParser());
        _documentRegistry.RegisterParser(new EpubBookParser());
    }

    private async Task OnStartOfBookReachedAsync()
    {
        try
        {
            StopReading();
            await MessageDialog.ShowAsync(this, "Start of the book reached");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing start of book dialog: {ex.Message}");
        }
    }

    private async Task OnEndOfBookReachedAsync()
    {
        try
        {
            StopReading();
            await MessageDialog.ShowAsync(this, "End of the book reached");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing end of book dialog: {ex.Message}");
        }
    }

    private void OpenFileButton_Click(object? sender, RoutedEventArgs e) => _ = OpenFileAsync();

    private async Task OpenFileAsync()
    {
        try
        {
            var filters = new List<Avalonia.Platform.Storage.FilePickerFileType>();
            foreach (var parser in _documentRegistry.AvailableParsers)
            {
                filters.Add(new Avalonia.Platform.Storage.FilePickerFileType(parser.FormatName)
                {
                    Patterns = parser.SupportedExtensions.Select(e => $"*{e}").ToList()
                });
            }

            var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Open Book",
                FileTypeFilter = filters
            };

            var result = await this.StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                SaveSettings();

                _currentBookFileName = result[0].Path.LocalPath;
                string bookName = Path.GetFileName(_currentBookFileName);

                _readerService.FullText = await _documentRegistry.LoadBookAsync(_currentBookFileName);

                var actualBook = _bookRecords.FirstOrDefault(r => r.Name == bookName);
                if (actualBook is null)
                {
                    actualBook = new BookRecord { Name = bookName };
                    _bookRecords.Add(actualBook);
                }

                _isUpdatingFromCode = true;
                _readerService.ResetPosition(actualBook.ScrollPosition);

                TextSlider.Maximum = _readerService.FullText.Length;
                TextSlider.Value = _readerService.CurrentPosition;
                BookNameText.Text = bookName;

                UpdateDisplayedText();
                UpdatePercentage();
                _isUpdatingFromCode = false;

                SaveSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening file: {ex.Message}");
        }
    }

    private void StartStopButton_Click(object? sender, RoutedEventArgs e) => ToggleStartStop();
    private void ReverseButton_Click(object? sender, RoutedEventArgs e) => _readerService.IsReversing = !_readerService.IsReversing;
    private void InfoButton_Click(object? sender, RoutedEventArgs e) => _ = ShowInfoAsync();

    private void ToggleStartStop()
    {
        if (_currentBookFileName == null) return;

        if (_readerService.IsPaused)
        {
            StartStopButton.Content = "Stop";
            _readerService.IsPaused = false;
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
        _readerService.IsPaused = true;
    }

    private async Task ShowInfoAsync()
    {
        try
        {
            var dialog = new InfoDialog();
            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing info: {ex.Message}");
        }
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left: _readerService.IsReversing = true; break;
            case Key.Right: _readerService.IsReversing = false; break;
            case Key.Space: ToggleStartStop(); break;
            case Key.Up:
                int upStep = (DateTime.Now - _lastKeyUpTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs 
                    ? AppConstants.LargeFontSizeStep 
                    : AppConstants.SmallFontSizeStep;
                _lastKeyUpTime = DateTime.Now;
                AdjustFontSize(upStep);
                break;
            case Key.Down:
                int downStep = (DateTime.Now - _lastKeyDownTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs 
                    ? -AppConstants.LargeFontSizeStep 
                    : -AppConstants.SmallFontSizeStep;
                _lastKeyDownTime = DateTime.Now;
                AdjustFontSize(downStep);
                break;
            case Key.R: _readerService.IsReversing = !_readerService.IsReversing; break;
            case Key.F: FadeCheckBox.IsChecked = !FadeCheckBox.IsChecked; break;
            case Key.S: SettingsExpander.IsExpanded = !SettingsExpander.IsExpanded; break;
            case Key.I: _ = ShowInfoAsync(); break;
            case Key.OemPlus: case Key.Add: SpeedSlider.Value += AppConstants.DefaultSpeedIncrement; break;
            case Key.OemMinus: case Key.Subtract: SpeedSlider.Value -= AppConstants.DefaultSpeedIncrement; break;
        }
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        SaveSettings();
        _gamepadService.Dispose();
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
        if (_readerService.IsPaused && !_isUpdatingFromCode)
        {
            _readerService.ResetPosition((int)TextSlider.Value);
            UpdateDisplayedText();
        }
    }

    private void FontSizeNumeric_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (!_isUpdatingFromCode && e.NewValue.HasValue)
        {
            MainTextBlock.FontSize = (double)e.NewValue.Value;
            ClearCharWidthCache();
            UpdateDisplayedText();

            // Force a re-center if paused
            if (_readerService.IsPaused)
            {
                Dispatcher.UIThread.Post(() => 
                {
                    if (ReadingAreaCanvas.Bounds.Height > 0 && MainTextBlock.Bounds.Height > 0)
                    {
                        double top = (ReadingAreaCanvas.Bounds.Height - MainTextBlock.Bounds.Height) / 2;
                        Canvas.SetTop(MainTextBlock, top);
                    }
                }, DispatcherPriority.Loaded);
            }
        }
    }
}
