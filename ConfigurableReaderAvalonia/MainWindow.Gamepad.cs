using System;
using Avalonia.Controls;

namespace ConfigurableReaderAvalonia;

public partial class MainWindow
{
    private void InitializeGamepad()
    {
        _gamepadService.InputModeChanged += isGamepad =>
        {
            if (InputModeText != null)
            {
                InputModeText.Text = isGamepad ? "🎮" : "⌨️";
                ToolTip.SetTip(InputModeText, isGamepad ? "Gamepad Mode" : "Keyboard Mode");
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
            int newPos;
            if (delta > 0)
            {
                newPos = Math.Min(_readerService.FullText.Length, _readerService.CurrentPosition + delta);
            }
            else
            {
                newPos = Math.Max(0, _readerService.CurrentPosition + delta);
            }
            _readerService.ResetPosition(newPos);
            UpdateDisplayedText();
        };

        _gamepadService.Start();
    }
}
