using System;
using System.Linq;
using Avalonia;
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
    private int _renderedBasePosition = -1;
    private TextLayout? _currentTextLayout;

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

            double pixelsToMove = SpeedSlider.Value * deltaTime;

            UpdateDisplayedText();
            _readerService.Advance(pixelsToMove, MapPixelsToPosition);
            UpdateRenderTransform();
            UpdatePercentage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnRendering: {ex.Message}");
        }
    }

    private (int newPos, double newOffset, bool eof) MapPixelsToPosition(int currentPos, double targetOffset)
    {
        if (_currentTextLayout == null || MainTextBlock.Text == null) return (currentPos, 0, false);

        int localIndex = Math.Clamp(currentPos - _renderedBasePosition, 0, MainTextBlock.Text.Length);
        
        var startRect = _currentTextLayout.HitTestTextPosition(localIndex);
        double absoluteTargetX = startRect.Left + targetOffset;

        var hit = _currentTextLayout.HitTestPoint(new Point(absoluteTargetX, 0));
        int newGlobalPos = _renderedBasePosition + hit.TextPosition;
        
        if (newGlobalPos >= _readerService.FullText.Length) return (_readerService.FullText.Length, 0, true);
        if (newGlobalPos < 0) return (0, 0, true);

        var newRect = _currentTextLayout.HitTestTextPosition(hit.TextPosition);
        double newSubOffset = absoluteTargetX - newRect.Left;

        return (newGlobalPos, newSubOffset, false);
    }

    private void UpdateRenderTransform()
    {
        if (_currentTextLayout == null || string.IsNullOrEmpty(_readerService.FullText) || MainTextBlock.Text == null) return;

        int localIndex = Math.Clamp(_readerService.CurrentPosition - _renderedBasePosition, 0, MainTextBlock.Text.Length);
        var rect = _currentTextLayout.HitTestTextPosition(localIndex);
        
        _textTranslateTransform.X = -(rect.Left - _readerService.CurrentOffsetX);

        if (ReadingAreaCanvas.Bounds.Height > 0)
        {
            double stableHeight = MainTextBlock.FontSize * AppConstants.VerticalCenteringMultiplier;
            if (MainTextBlock.Height != stableHeight) MainTextBlock.Height = stableHeight;
            double top = (ReadingAreaCanvas.Bounds.Height - stableHeight) / 2;
            Canvas.SetTop(MainTextBlock, top);
        }
    }

    private void UpdateDisplayedText()
    {
        if (string.IsNullOrEmpty(_readerService.FullText)) return;

        const int safeZone = 2000;
        bool needsUpdate = _renderedBasePosition == -1 || 
                           _readerService.CurrentPosition < _renderedBasePosition ||
                           _readerService.CurrentPosition > _renderedBasePosition + AppConstants.MaxBufferLength - safeZone;

        if (needsUpdate)
        {
            _renderedBasePosition = Math.Max(0, _readerService.CurrentPosition - safeZone);
            int length = Math.Min(AppConstants.MaxBufferLength, _readerService.FullText.Length - _renderedBasePosition);
            string newText = _readerService.FullText.Substring(_renderedBasePosition, length);

            if (MainTextBlock.Text != newText)
            {
                MainTextBlock.Text = newText;
                
                var typeface = new Typeface(MainTextBlock.FontFamily, MainTextBlock.FontStyle, MainTextBlock.FontWeight);
                _currentTextLayout = new TextLayout(
                    newText,
                    typeface,
                    MainTextBlock.FontSize,
                    MainTextBlock.Foreground);
            }
        }

        _isUpdatingFromCode = true;
        TextSlider.Value = _readerService.CurrentPosition;
        _isUpdatingFromCode = false;
    }

    private double GetCharacterWidth(char c)
    {
        var typeface = new Typeface(MainTextBlock.FontFamily, MainTextBlock.FontStyle, MainTextBlock.FontWeight);
        var textLayout = new TextLayout(c.ToString(), typeface, MainTextBlock.FontSize, MainTextBlock.Foreground);
        return textLayout.TextLines[0].Width;
    }

    private void ClearCharWidthCache() { }

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
        
        _renderedBasePosition = -1; 
        UpdateDisplayedText();
        UpdateRenderTransform();
    }
}
