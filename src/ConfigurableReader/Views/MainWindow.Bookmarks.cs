using Avalonia.Controls;
using Avalonia.Interactivity;
using ConfigurableReader.Core;
using System.Threading.Tasks;

namespace ConfigurableReader.Views;

public partial class MainWindow
{
    private void TocButton_Click(object? sender, RoutedEventArgs e)
    {
        TocBookmarksPanel.IsVisible = !TocBookmarksPanel.IsVisible;
    }

    private void TocTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TocTreeView.SelectedItem is BookmarkItem item)
        {
            _ = JumpToBookmarkAsync(item.Position);
            TocTreeView.SelectedItem = null;
        }
    }

    private void BookmarksListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BookmarksListBox.SelectedItem is BookmarkItem item)
        {
            _ = JumpToBookmarkAsync(item.Position);
            BookmarksListBox.SelectedItem = null;
        }
    }

    private async Task JumpToBookmarkAsync(int position)
    {
        using (_controller.SuppressCodeUpdates())
        {
            await _readerService.ResetPositionAsync(position);
            _renderedBasePosition = -1; // Force re-render

            TextSlider.Value = _readerService.CurrentPosition;
            UpdateDisplayedText();
            UpdateRenderTransform();
            UpdatePercentage();
        }
    }

    private void AddBookmarkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controller.CurrentRecord == null) return;

        string name = string.IsNullOrWhiteSpace(BookmarkNameTextBox.Text)
            ? $"Bookmark at {_readerService.CurrentPosition}"
            : BookmarkNameTextBox.Text;

        var bookmark = new BookmarkItem
        {
            Title = name,
            Position = _readerService.CurrentPosition
        };

        _controller.CurrentRecord.CustomBookmarks.Add(bookmark);

        if (BookmarksListBox.ItemsSource == null)
            BookmarksListBox.ItemsSource = _controller.CurrentRecord.CustomBookmarks;

        _controller.SaveCurrentPosition(); // Saves bookmarks too
        BookmarkNameTextBox.Text = string.Empty;
    }

    private void DeleteBookmarkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is BookmarkItem item && _controller.CurrentRecord != null)
        {
            _controller.CurrentRecord.CustomBookmarks.Remove(item);
            _controller.SaveCurrentPosition();
        }
    }
}
