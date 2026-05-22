using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace ConfigurableReader.Views;

using ConfigurableReader.Services;

public partial class MainWindow
{
    private void InitializeGamepad()
    {
        _gamepadService.InputModeChanged += isGamepad =>
        {
            if (InputModeText != null)
            {
                InputModeText.Text = isGamepad ? "🎮" : "⌨️";
                ToolTip.SetTip(InputModeText, isGamepad ? LocalizationService.GetString("GamepadMode") : LocalizationService.GetString("KeyboardMode"));
            }
        };

        _gamepadService.ToggleStartStopRequested += ToggleStartStop;
        
        _gamepadService.ToggleReverseRequested += () => 
        {
            _readerService.IsReversing = !_readerService.IsReversing;
        };

        _gamepadService.ToggleFadeRequested += () => 
        {
            FadeCheckBox.IsChecked = !FadeCheckBox.IsChecked;
        };

        _gamepadService.ToggleSettingsRequested += () =>
        {
            SettingsExpander.IsExpanded = !SettingsExpander.IsExpanded;
        };

        _gamepadService.ShowInfoRequested += () => _ = ShowInfoAsync();

        _gamepadService.FontSizeAdjustmentRequested += AdjustFontSize;

        _gamepadService.SpeedAdjustmentRequested += delta => 
        {
            SpeedSlider.Value += delta;
        };

        _gamepadService.SetReverseDirectionRequested += value => 
        {
            _readerService.IsReversing = value;
        };

        _gamepadService.PositionAdjustmentRequested += delta =>
        {
            _ = HandlePositionAdjustmentAsync(delta);
        };

        _gamepadService.Start();
    }

    private async Task HandlePositionAdjustmentAsync(int delta)
    {
        try
        {
            int newPos;
            if (delta > 0)
            {
                newPos = Math.Min(_readerService.TotalLength, _readerService.CurrentPosition + delta);
            }
            else
            {
                newPos = Math.Max(0, _readerService.CurrentPosition + delta);
            }
            await _readerService.ResetPositionAsync(newPos);
            UpdateDisplayedText();
            UpdateRenderTransform();
            UpdatePercentage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adjusting position via gamepad: {ex.Message}");
        }
    }
}
