using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ConfigurableReader.Common;
using ConfigurableReader.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        if (e.Key == Key.Escape && _isZenMode)
        {
            ToggleZenMode();
            return;
        }

        var action = _settings.KeyBindings.GetActionForKey(e.Key);
        if (action == null) return;

        switch (action.Value)
        {
            case ReaderAction.DirectionBackward: _readerService.IsReversing = true; break;
            case ReaderAction.DirectionForward: _readerService.IsReversing = false; break;
            case ReaderAction.TogglePlayPause: ToggleStartStop(); break;
            case ReaderAction.FontSizeIncrease:
                int upStep = (DateTime.Now - _lastKeyUpTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs
                    ? AppConstants.LargeFontSizeStep
                    : AppConstants.SmallFontSizeStep;
                _lastKeyUpTime = DateTime.Now;
                AdjustFontSize(upStep);
                break;
            case ReaderAction.FontSizeDecrease:
                int downStep = (DateTime.Now - _lastKeyDownTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs
                    ? -AppConstants.LargeFontSizeStep
                    : -AppConstants.SmallFontSizeStep;
                _lastKeyDownTime = DateTime.Now;
                AdjustFontSize(downStep);
                break;
            case ReaderAction.ToggleReverse: _readerService.IsReversing = !_readerService.IsReversing; break;
            case ReaderAction.ToggleEdgeFade: FadeCheckBox.IsChecked = !FadeCheckBox.IsChecked; break;
            case ReaderAction.ToggleSettings: SettingsExpander.IsExpanded = !SettingsExpander.IsExpanded; break;
            case ReaderAction.CycleTheme: CycleNextTheme(); break;
            case ReaderAction.ToggleReadingMode: ToggleReadingMode(); break;
            case ReaderAction.OpenWebpage: _ = OpenWebpageAsync(); break;
            case ReaderAction.ShowInfo: _ = ShowInfoAsync(); break;
            case ReaderAction.ToggleZenMode: ToggleZenMode(); break;
            case ReaderAction.ConfigureKeybindings: _ = ShowKeybindingsAsync(); break;
            case ReaderAction.SpeedIncrease:
                if (_settings.ReadingMode == "RSVP")
                {
                    RsvpWpmNumeric.Value = Math.Min(2000, (RsvpWpmNumeric.Value ?? 300) + 25);
                }
                else
                {
                    SpeedSlider.Value += AppConstants.DefaultSpeedIncrement;
                }
                break;
            case ReaderAction.SpeedDecrease:
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

    private async Task ShowKeybindingsAsync()
    {
        var dialog = new KeybindingsDialog(_settings.KeyBindings);
        var result = await dialog.ShowDialog<KeyBindingsConfig?>(this);
        if (result != null)
        {
            _settings.KeyBindings = result;
            _settings.Save();
        }
    }

    private void KeybindingsButton_Click(object? sender, RoutedEventArgs e)
    {
        _ = ShowKeybindingsAsync();
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
