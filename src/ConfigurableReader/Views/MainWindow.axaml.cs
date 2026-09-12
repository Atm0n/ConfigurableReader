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
using ConfigurableReader.Core;
using System.Collections.Generic;

namespace ConfigurableReader.Views;

using ConfigurableReader.Models;
using ConfigurableReader.Common;
using ConfigurableReader.Services;

public partial class MainWindow : Window
{
    private readonly GamepadService _gamepadService = new();
    private readonly ReaderService _readerService = new();
    private readonly DocumentRegistry _documentRegistry;
    private readonly ReaderController _controller;
    
    private string? _currentBookFileName => _controller.CurrentBookFilePath;
    private bool _isUpdatingFromCode => _controller.IsUpdatingFromCode;

    private DateTime _lastKeyUpTime = DateTime.MinValue;
    private DateTime _lastKeyDownTime = DateTime.MinValue;

    /// <summary>
    /// Parameterless constructor required by Avalonia XAML compiler and designer.
    /// </summary>
    public MainWindow() : this(new DocumentRegistry())
    {
    }

    public MainWindow(DocumentRegistry documentRegistry)
    {
        _documentRegistry = documentRegistry;
        _controller = new ReaderController(documentRegistry, _readerService);

        InitializeComponent();

        ShowLibrary();

        _settings = AppSettings.Load();
        LoadBookPositionConfiguration();
        PopulateFontList();
        ApplySettings();

        InitializeRendering();
        InitializeGamepad();

        AddHandler(DragDrop.DragOverEvent, Window_DragOver);
        AddHandler(DragDrop.DropEvent, Window_Drop);

        _readerService.StartOfBookReached += () => 
        {
            Dispatcher.UIThread.Post(() => _ = OnStartOfBookReachedAsync());
        };

        _readerService.EndOfBookReached += () => 
        {
            Dispatcher.UIThread.Post(() => _ = OnEndOfBookReachedAsync());
        };
    }

    private async Task OnStartOfBookReachedAsync()
    {
        try
        {
            StopReading();
            await MessageDialog.ShowAsync(this, LocalizationService.GetString("StartReached"));
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
            await MessageDialog.ShowAsync(this, LocalizationService.GetString("EndReached"));
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
            
            // Add "All Supported Books" combined filter
            var allExtensions = _documentRegistry.AvailableParsers
                .SelectMany(p => p.SupportedExtensions)
                .Select(e => $"*{e}")
                .ToList();
            
            if (allExtensions.Any())
            {
                filters.Add(new Avalonia.Platform.Storage.FilePickerFileType(LocalizationService.GetString("AllSupportedBooks"))
                {
                    Patterns = allExtensions
                });
            }

            // Add individual filters
            foreach (var parser in _documentRegistry.AvailableParsers)
            {
                filters.Add(new Avalonia.Platform.Storage.FilePickerFileType(parser.FormatName)
                {
                    Patterns = parser.SupportedExtensions.Select(e => $"*{e}").ToList()
                });
            }

            var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = LocalizationService.GetString("OpenFileTitle"),
                FileTypeFilter = filters
            };

            var result = await this.StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                await LoadBookAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            await MessageDialog.ShowAsync(this, $"{LocalizationService.GetString("CouldNotOpenFile")} {ex.Message}");
        }
    }

    private async Task LoadBookAsync(string filePath)
    {
        try
        {
            using (_controller.SuppressCodeUpdates())
            {
                _renderedBasePosition = -1; // Force re-render of the text buffer
                
                string bookName = await _controller.OpenBookAsync(filePath);

                TextSlider.Maximum = _readerService.TotalLength;
                TextSlider.Value = _readerService.CurrentPosition;
                BookNameText.Text = bookName;

                // Bind TOC and Bookmarks
                TocTreeView.ItemsSource = _readerService.CurrentSource?.TableOfContents;
                if (_controller.CurrentRecord != null)
                {
                    BookmarksListBox.ItemsSource = null;
                    BookmarksListBox.ItemsSource = _controller.CurrentRecord.CustomBookmarks;
                }

                UpdateDisplayedText();
                UpdatePercentage();
                
                ShowReader();
            }
        }
        catch (Exception ex)
        {
            await MessageDialog.ShowAsync(this, $"{LocalizationService.GetString("FailedToLoadBook")}\n{ex.Message}");
            ShowLibrary();
        }
    }

    private void StartStopButton_Click(object? sender, RoutedEventArgs e) => ToggleStartStop();
    private void ReverseButton_Click(object? sender, RoutedEventArgs e) => _readerService.IsReversing = !_readerService.IsReversing;
    private void SearchButton_Click(object? sender, RoutedEventArgs e) => _ = PerformSearchAsync();
    private void InfoButton_Click(object? sender, RoutedEventArgs e) => _ = ShowInfoAsync();

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTextBox.Text)) return;

        try
        {
            string query = SearchTextBox.Text;
            // Search starting from just after the current position
            int foundIndex = await _readerService.FindNextAsync(query, _readerService.CurrentPosition + 1);

            // If not found, wrap around to the beginning
            if (foundIndex == -1)
            {
                foundIndex = await _readerService.FindNextAsync(query, 0);
            }

            if (foundIndex != -1)
            {
                StopReading();
                using (_controller.SuppressCodeUpdates())
                {
                    await _readerService.ResetPositionAsync(foundIndex);
                    _renderedBasePosition = -1; // Force re-render
                    
                    TextSlider.Value = _readerService.CurrentPosition;
                    UpdateDisplayedText();
                    UpdateRenderTransform();
                    UpdatePercentage();
                }
            }
            else
            {
                string message = string.Format(LocalizationService.GetString("SearchNoResults"), query);
                await MessageDialog.ShowAsync(this, message);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}");
            await MessageDialog.ShowAsync(this, $"Error: {ex.Message}");
        }
    }

    private void ToggleStartStop()
    {
        if (_currentBookFileName == null) return;

        if (_readerService.IsPaused)
        {
            StartStopButton.Content = LocalizationService.GetString("Stop");
            _readerService.IsPaused = false;
            StartAnimationLoop();
            SettingsExpander.IsExpanded = false;
        }
        else
        {
            StopReading();
        }
    }

    private void StopReading()
    {
        StartStopButton.Content = LocalizationService.GetString("Start");
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

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        SaveSettings();
        _readerService.Dispose();
        _gamepadService.Dispose();
    }

    private void TextSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _ = TextSlider_ValueChangedAsync(sender, e);
    }

    private async Task TextSlider_ValueChangedAsync(object? sender, RangeBaseValueChangedEventArgs e)
    {
        try
        {
            if (!_isUpdatingFromCode)
            {
                await _readerService.ResetPositionAsync((int)TextSlider.Value);
                _renderedBasePosition = -1;
                UpdateDisplayedText();
                UpdateRenderTransform();
                UpdatePercentage();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating text from slider: {ex.Message}");
        }
    }

    private void FontSizeNumeric_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (!_isUpdatingFromCode && e.NewValue.HasValue)
        {
            MainTextBlock.FontSize = (double)e.NewValue.Value;
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
