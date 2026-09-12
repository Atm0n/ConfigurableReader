using Avalonia.Controls;
using Avalonia.Interactivity;
using ConfigurableReader.Models;
using ConfigurableReader.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConfigurableReader.Views;

public partial class MainWindow
{
    private void ShowLibrary()
    {
        StopReading();
        LibraryViewContainer.IsVisible = true;
        ReaderViewContainer.IsVisible = false;

        if (LibrarySortComboBox != null && LibrarySortComboBox.SelectedItem == null)
        {
            using (_controller.SuppressCodeUpdates())
            {
                var match = LibrarySortComboBox.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == _settings.LibrarySortOption);
                LibrarySortComboBox.SelectedItem = match ?? LibrarySortComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault();
            }
        }

        ApplyLibraryFilter();
    }

    private void ApplyLibraryFilter()
    {
        string query = LibrarySearchTextBox?.Text?.Trim() ?? string.Empty;
        var records = _controller.BookRecords.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
        {
            records = records.Where(b =>
                b.DisplayTitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(b.FilePath).Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        string sortKey = (LibrarySortComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() 
            ?? _settings.LibrarySortOption 
            ?? "Recent";

        records = sortKey switch
        {
            "TitleAsc" => records.OrderBy(b => b.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
            "TitleDesc" => records.OrderByDescending(b => b.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
            "ProgressDesc" => records.OrderByDescending(b => b.ProgressPercentage).ThenByDescending(b => b.LastReadDate),
            "ProgressAsc" => records.OrderBy(b => b.ProgressPercentage).ThenByDescending(b => b.LastReadDate),
            _ => records.OrderByDescending(b => b.LastReadDate)
        };

        var list = records.ToList();
        LibraryItemsControl.ItemsSource = null;
        LibraryItemsControl.ItemsSource = list;

        if (EmptyLibraryText != null)
        {
            EmptyLibraryText.IsVisible = list.Count == 0;
        }
    }

    private void LibrarySearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyLibraryFilter();
    }

    private void LibrarySortComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_controller.IsUpdatingFromCode) return;
        if (LibrarySortComboBox?.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            _settings.LibrarySortOption = item.Tag.ToString() ?? "Recent";
            _settings.Save();
        }
        ApplyLibraryFilter();
    }

    private void ShowReader()
    {
        LibraryViewContainer.IsVisible = false;
        ReaderViewContainer.IsVisible = true;
    }

    private void BackToLibraryButton_Click(object? sender, RoutedEventArgs e)
    {
        _controller.SaveCurrentPosition();
        ShowLibrary();
    }

    private void LibraryBook_Click(object? sender, RoutedEventArgs e)
    {
        _ = LibraryBook_ClickAsync(sender, e);
    }

    private async Task LibraryBook_ClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is BookRecord record)
            {
                if (record.IsWebArticle || File.Exists(record.FilePath))
                {
                    await LoadBookAsync(record.FilePath);
                }
                else
                {
                    await MessageDialog.ShowAsync(this, LocalizationService.GetString("FileNotFound"));
                    _controller.RemoveBookRecord(record);
                    ShowLibrary();
                }
            }
        }
        catch (Exception ex)
        {
            await MessageDialog.ShowAsync(this, $"{LocalizationService.GetString("Error")}: {ex.Message}");
        }
    }

    private void RemoveBookFromLibrary_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is BookRecord record)
        {
            _controller.RemoveBookRecord(record);
            ShowLibrary();
        }
    }
}
