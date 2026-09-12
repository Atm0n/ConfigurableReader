using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;

namespace ConfigurableReader.Views;

public partial class OpenUrlDialog : Window
{
    private string? _resultUrl;

    public OpenUrlDialog()
    {
        InitializeComponent();

        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        _ = TryPopulateFromClipboardAsync();
        UrlTextBox.Focus();
        UrlTextBox.SelectAll();
    }

    private async Task TryPopulateFromClipboardAsync()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                var data = await clipboard.TryGetDataAsync();
                if (data != null)
                {
                    string? text = await data.TryGetTextAsync();
                    if (!string.IsNullOrWhiteSpace(text) &&
                        (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    {
                        UrlTextBox.Text = text.Trim();
                        UrlTextBox.SelectAll();
                    }
                }
            }
        }
        catch
        {
            // Clipboard access might fail on some platforms/permissions
        }
    }

    public static async Task<string?> ShowAsync(Window owner)
    {
        var dialog = new OpenUrlDialog();
        await dialog.ShowDialog(owner);
        return dialog._resultUrl;
    }

    private void ReadButton_Click(object? sender, RoutedEventArgs e)
    {
        Submit();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        _resultUrl = null;
        Close();
    }

    private void UrlTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Submit();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _resultUrl = null;
            Close();
            e.Handled = true;
        }
    }

    private void Submit()
    {
        string text = UrlTextBox.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                text = "https://" + text;
            }

            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                _resultUrl = text;
                Close();
                return;
            }
        }

        // Invalid URL highlight
        UrlTextBox.BorderBrush = Avalonia.Media.Brushes.Red;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            _resultUrl = null;
            Close();
        }
    }
}
