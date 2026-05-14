using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace ConfigurableReader;

public partial class MainWindow
{
    private readonly DispatcherTimer _timer;
    private readonly TranslateTransform _textTranslateTransform = new();
    private DateTime _lastRenderTime;
    private readonly Dictionary<char, double> _charWidths = [];

    private void InitializeRendering()
    {
        _timer.Tick += (s, e) =>
        {
            _ = OnRenderingAsync();
        };
        
        MainTextBlock.RenderTransform = _textTranslateTransform;
        _timer.Start();
    }

    private async Task OnRenderingAsync()
    {
        try
        {
            if (_readerService.IsPaused || string.IsNullOrEmpty(_readerService.FullText)) return;

            DateTime now = DateTime.Now;
            if (_lastRenderTime == DateTime.MinValue)
            {
                _lastRenderTime = now;
                return;
            }

            double deltaTime = (now - _lastRenderTime).TotalSeconds;
            _lastRenderTime = now;

            _readerService.Update(deltaTime, SpeedSlider.Value, GetCharacterWidth);

            _textTranslateTransform.X = _readerService.CurrentOffsetX;

            // Centering logic
            if (ReadingAreaCanvas.Bounds.Height > 0 && MainTextBlock.Bounds.Height > 0)
            {
                double top = (ReadingAreaCanvas.Bounds.Height - MainTextBlock.Bounds.Height) / 2;
                Canvas.SetTop(MainTextBlock, top);
            }

            UpdateDisplayedText();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnRendering: {ex.Message}");
        }
    }

    private void UpdateDisplayedText()
    {
        int length = Math.Min(5000, _readerService.FullText.Length - _readerService.CurrentPosition);
        string newText = _readerService.FullText.Substring(_readerService.CurrentPosition, length);

        if (MainTextBlock.Text != newText)
        {
            MainTextBlock.Text = newText;
        }

        _isUpdatingFromCode = true;
        TextSlider.Value = _readerService.CurrentPosition;
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

    private void UpdatePercentage()
    {
        if (_readerService.FullText.Length > 0)
        {
            double percentage = (double)_readerService.CurrentPosition / _readerService.FullText.Length * 100;
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
        
        // Update UI immediately
        UpdateDisplayedText();
        
        // Centering logic needs to be triggered manually if paused
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
