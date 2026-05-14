using System;
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
    private readonly double[] _charWidthCache = new double[65536];
    private int _renderedBasePosition = -1;

    private void InitializeRendering()
    {
        _timer.Tick += OnRendering;
        
        MainTextBlock.RenderTransform = _textTranslateTransform;
        _timer.Start();
    }

    private void OnRendering(object? sender, EventArgs e)
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

            // 1. Ensure the display buffer is current
            UpdateDisplayedText();

            // 2. Additive Prefix Calculation:
            // Calculate distance from CURRENT logical start of TextBlock (_renderedBasePosition)
            // to the CURRENT scrolling start point (_readerService.CurrentPosition).
            double prefixWidth = 0;
            int start = Math.Min(_renderedBasePosition, _readerService.CurrentPosition);
            int end = Math.Max(_renderedBasePosition, _readerService.CurrentPosition);
            
            for (int i = start; i < end; i++)
            {
                prefixWidth += GetCharacterWidth(_readerService.FullText[i]);
            }

            if (_readerService.CurrentPosition < _renderedBasePosition)
                prefixWidth = -prefixWidth;

            // 3. Apply the transform relative to the current buffer start.
            _textTranslateTransform.X = -(prefixWidth - _readerService.CurrentOffsetX);

            // Stable centering logic
            if (ReadingAreaCanvas.Bounds.Height > 0)
            {
                double stableHeight = MainTextBlock.FontSize * AppConstants.VerticalCenteringMultiplier;
                if (MainTextBlock.Height != stableHeight) MainTextBlock.Height = stableHeight;
                
                double top = (ReadingAreaCanvas.Bounds.Height - stableHeight) / 2;
                Canvas.SetTop(MainTextBlock, top);
            }

            UpdatePercentage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnRendering: {ex.Message}");
        }
    }

    private void UpdateDisplayedText()
    {
        if (string.IsNullOrEmpty(_readerService.FullText)) return;

        // Large buffer and infrequent updates for maximum stability.
        bool needsUpdate = _renderedBasePosition == -1 || 
                           Math.Abs(_readerService.CurrentPosition - _renderedBasePosition) > AppConstants.BufferUpdateThreshold;

        if (needsUpdate)
        {
            // Mathematical Continuity:
            // When we jump the base position, we don't need to do anything special
            // because OnRendering recalculates X relative to the new _renderedBasePosition
            // in the same frame.
            _renderedBasePosition = _readerService.CurrentPosition;
            int length = Math.Min(AppConstants.MaxBufferLength, _readerService.FullText.Length - _renderedBasePosition);
            string newText = _readerService.FullText.Substring(_renderedBasePosition, length);

            if (MainTextBlock.Text != newText)
            {
                MainTextBlock.Text = newText;
            }
        }

        _isUpdatingFromCode = true;
        TextSlider.Value = _readerService.CurrentPosition;
        _isUpdatingFromCode = false;
    }

    private double GetCharacterWidth(char c)
    {
        // Use a fast array-based cache for O(1) lookups
        if (_charWidthCache[c] > 0) return _charWidthCache[c];

        var typeface = new Typeface(MainTextBlock.FontFamily, MainTextBlock.FontStyle, MainTextBlock.FontWeight);

        var textLayout = new TextLayout(
            c.ToString(),
            typeface,
            MainTextBlock.FontSize,
            MainTextBlock.Foreground);

        double width = textLayout.TextLines[0].Width;
        _charWidthCache[c] = width;
        return width;
    }

    private void ClearCharWidthCache()
    {
        Array.Clear(_charWidthCache, 0, _charWidthCache.Length);
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
        MainTextBlock.FontSize = Math.Clamp(newSize, AppConstants.MinFontSize, AppConstants.MaxFontSize);
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
