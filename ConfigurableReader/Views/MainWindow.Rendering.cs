using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace ConfigurableReader.Views;

using ConfigurableReader.Common;

public partial class MainWindow
{
    private readonly DispatcherTimer _timer;
    private readonly TranslateTransform _textTranslateTransform = new();
    private DateTime _lastRenderTime;
    private int _renderedBasePosition = -1;
    private string? _currentRenderedText;
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
            if (_readerService.IsPaused || string.IsNullOrEmpty(_readerService.BufferText)) return;

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

        int localIndex = Math.Clamp(currentPos - _renderedBasePosition, 0, _currentRenderedText?.Length ?? 0);
        
        var startRect = _currentTextLayout.HitTestTextPosition(localIndex);
        double absoluteTargetX = startRect.Left + targetOffset;

        var hit = _currentTextLayout.HitTestPoint(new Point(absoluteTargetX, 0));
        int newGlobalPos = _renderedBasePosition + hit.TextPosition;
        
        if (newGlobalPos >= _readerService.TotalLength) return (_readerService.TotalLength, 0, true);
        if (newGlobalPos < 0) return (0, 0, true);

        var newRect = _currentTextLayout.HitTestTextPosition(hit.TextPosition);
        double newSubOffset = absoluteTargetX - newRect.Left;

        return (newGlobalPos, newSubOffset, false);
    }

    private void UpdateRenderTransform()
    {
        if (_currentTextLayout == null || string.IsNullOrEmpty(_readerService.BufferText) || MainTextBlock.Text == null) return;

        int localIndex = Math.Clamp(_readerService.CurrentPosition - _renderedBasePosition, 0, _currentRenderedText?.Length ?? 0);
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
        if (string.IsNullOrEmpty(_readerService.BufferText)) return;

        // Ensure current position is within the buffer before attempting to render
        if (_readerService.CurrentPosition < _readerService.BufferStartPosition || 
            _readerService.CurrentPosition >= _readerService.BufferStartPosition + _readerService.BufferText.Length)
        {
            return;
        }

        const int safeZone = 2000;
        bool needsUpdate = _renderedBasePosition == -1 || 
                           _readerService.CurrentPosition < _renderedBasePosition ||
                           _readerService.CurrentPosition > _renderedBasePosition + AppConstants.MaxBufferLength - safeZone;

        if (needsUpdate)
        {
            // _renderedBasePosition must be relative to the buffer for substring to work,
            // OR we map absolute to relative. Let's keep _renderedBasePosition as absolute.
            _renderedBasePosition = Math.Max(_readerService.BufferStartPosition, _readerService.CurrentPosition - safeZone);
            
            int relativeBase = _renderedBasePosition - _readerService.BufferStartPosition;
            int length = Math.Min(AppConstants.MaxBufferLength, _readerService.BufferText.Length - relativeBase);
            
            if (length > 0)
            {
                string newText = _readerService.BufferText.Substring(relativeBase, length);

                if (_currentRenderedText != newText)
                {
                    _currentRenderedText = newText;
                    
                    if (_settings.SpeedReadingMode)
                    {
                        MainTextBlock.Text = newText;
                        if (MainTextBlock.Inlines != null)
                        {
                            MainTextBlock.Inlines.Clear();
                            var runs = ConfigurableReader.Core.SpeedReadingProcessor.ProcessText(newText, _settings.SpeedReadingBoldRatio);
                            MainTextBlock.Inlines.AddRange(runs);
                        }

                        var typeface = new Typeface(MainTextBlock.FontFamily, MainTextBlock.FontStyle, MainTextBlock.FontWeight);
                        var boldTypeface = new Typeface(MainTextBlock.FontFamily, MainTextBlock.FontStyle, FontWeight.Bold);
                        
                        var overrides = new System.Collections.Generic.List<global::Avalonia.Utilities.ValueSpan<TextRunProperties>>();
                        var boldProperties = new GenericTextRunProperties(boldTypeface, MainTextBlock.FontSize, null, MainTextBlock.Foreground);
                        
                        var matches = System.Text.RegularExpressions.Regex.Matches(newText, @"(\p{L}+)|([^\p{L}]+)");
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            if (match.Groups[1].Success) // It's a word
                            {
                                string word = match.Value;
                                int boldLength = System.Math.Clamp((int)System.Math.Ceiling(word.Length * _settings.SpeedReadingBoldRatio), 1, word.Length);
                                overrides.Add(new global::Avalonia.Utilities.ValueSpan<TextRunProperties>(match.Index, boldLength, boldProperties));
                            }
                        }
                        
                        _currentTextLayout = new TextLayout(
                            newText,
                            typeface,
                            MainTextBlock.FontSize,
                            MainTextBlock.Foreground,
                            textStyleOverrides: overrides);
                    }
                    else
                    {
                        if (MainTextBlock.Inlines != null) MainTextBlock.Inlines.Clear();
                        MainTextBlock.Text = newText;
                        
                        var typeface = new Typeface(MainTextBlock.FontFamily, MainTextBlock.FontStyle, MainTextBlock.FontWeight);
                        _currentTextLayout = new TextLayout(
                            newText,
                            typeface,
                            MainTextBlock.FontSize,
                            MainTextBlock.Foreground);
                    }
                }
            }
        }

        using (_controller.SuppressCodeUpdates())
        {
            TextSlider.Value = _readerService.CurrentPosition;
        }
    }


    private void UpdatePercentage()
    {
        if (_readerService.TotalLength > 0)
        {
            double percentage = (double)_readerService.CurrentPosition / _readerService.TotalLength * 100;
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
