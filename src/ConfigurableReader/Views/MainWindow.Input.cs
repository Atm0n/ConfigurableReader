using Avalonia.Controls;
using Avalonia.Input;
using ConfigurableReader.Common;
using System;
using System.Linq;

namespace ConfigurableReader.Views;

public partial class MainWindow
{
    private bool _isZenMode = false;

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ignore global shortcuts if a text input has focus
        if (FocusManager?.GetFocusedElement() is TextBox)
        {
            return;
        }

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
            case Key.T: CycleNextTheme(); break;
            case Key.M: ToggleReadingMode(); break;
            case Key.U: _ = OpenWebpageAsync(); break;
            case Key.I: _ = ShowInfoAsync(); break;
            case Key.F11: ToggleZenMode(); break;
            case Key.Escape: if (_isZenMode) ToggleZenMode(); break;
            case Key.OemPlus: case Key.Add:
                if (_settings.ReadingMode == "RSVP")
                {
                    RsvpWpmNumeric.Value = Math.Min(2000, (RsvpWpmNumeric.Value ?? 300) + 25);
                }
                else
                {
                    SpeedSlider.Value += AppConstants.DefaultSpeedIncrement;
                }
                break;
            case Key.OemMinus: case Key.Subtract:
                if (_settings.ReadingMode == "RSVP")
                {
                    RsvpWpmNumeric.Value = Math.Max(50, (RsvpWpmNumeric.Value ?? 300) - 25);
                }
                else
                {
                    SpeedSlider.Value -= AppConstants.DefaultSpeedIncrement;
                }
                break;
        }
    }

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = PerformSearchAsync();
            e.Handled = true;
        }
    }

    private void ReadingAreaCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            AdjustFontSize(e.Delta.Y > 0 ? 2 : -2);
            e.Handled = true;
        }
        else if (_currentBookFileName != null)
        {
            int delta = (int)(e.Delta.Y * -150);
            _ = HandlePositionAdjustmentAsync(delta);
            e.Handled = true;
        }
    }

    private void ToggleZenMode()
    {
        _isZenMode = !_isZenMode;
        if (_isZenMode)
        {
            WindowState = WindowState.FullScreen;
            SettingsExpander.IsVisible = false;
            BottomControlBar.IsVisible = false;
        }
        else
        {
            WindowState = WindowState.Maximized;
            SettingsExpander.IsVisible = true;
            BottomControlBar.IsVisible = true;
        }
    }

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File) || e.DataTransfer.Contains(DataFormat.Text))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Window_Drop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files != null)
        {
            var supportedFile = files.FirstOrDefault(f => _documentRegistry.GetParserForFile(f.Path.LocalPath) != null);
            if (supportedFile != null)
            {
                _ = LoadBookAsync(supportedFile.Path.LocalPath);
                return;
            }
        }

        string? text = e.DataTransfer.TryGetText();
        if (!string.IsNullOrWhiteSpace(text))
        {
            string trimmed = text.Trim();
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _ = LoadBookAsync(trimmed);
            }
        }
    }
}
