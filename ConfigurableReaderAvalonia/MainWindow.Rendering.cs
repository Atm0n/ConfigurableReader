using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace ConfigurableReaderAvalonia;

public partial class MainWindow
{
    private readonly DispatcherTimer _timer;
    private readonly TranslateTransform _textTranslateTransform = new();
    private double _currentOffsetX = 0;
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
            if (_isPaused || string.IsNullOrEmpty(_fullText)) return;

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

            if (_isReversing)
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
                        await MessageDialog.ShowAsync(this, "Start of the book reached");
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
                        await MessageDialog.ShowAsync(this, "End of the book reached");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnRendering: {ex.Message}");
        }
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
}
