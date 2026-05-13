using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using DevDecoder.HIDDevices.Converters;

namespace ConfigurableReaderAvalonia;

public partial class MainWindow
{
    private readonly Devices _devices = new();
    private IDisposable? _gamepadSubscription;
    private readonly HashSet<Gamepad> _activeGamepads = [];

    private bool _lastButtonAState = false;
    private bool _lastButtonBState = false;
    private bool _lastButtonXState = false;
    private bool _lastButtonYState = false;
    private bool _lastDPadUpState = false;
    private bool _lastDPadDownState = false;
    private bool _lastDPadLeftState = false;
    private bool _lastDPadRightState = false;
    private bool _lastLBState = false;
    private bool _lastRBState = false;

    private DateTime _lastLTPressTime = DateTime.MinValue;
    private DateTime _lastRTPressTime = DateTime.MinValue;
    private bool _ltWasDown = false;
    private bool _rtWasDown = false;
    private bool _ltBoosted = false;
    private bool _rtBoosted = false;

    private DateTime _lastDPadUpTime = DateTime.MinValue;
    private DateTime _lastDPadDownTime = DateTime.MinValue;

    private void InitializeGamepad()
    {
        _gamepadSubscription = _devices.Controllers<Gamepad>().Subscribe(gamepad =>
        {
            gamepad.Connect();

            gamepad.ConnectionState.Subscribe(isConnected =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (isConnected)
                        _activeGamepads.Add(gamepad);
                    else
                        _activeGamepads.Remove(gamepad);
                    UpdateInputModeIndicator();
                });
            });

            gamepad.Changes.Subscribe(_ =>
            {
                Dispatcher.UIThread.Post(() => HandleGamepadInput(gamepad));
            });
        });
    }

    private void UpdateInputModeIndicator()
    {
        if (InputModeText != null)
        {
            InputModeText.Text = _activeGamepads.Count > 0 ? "🎮" : "⌨️";
            ToolTip.SetTip(InputModeText, _activeGamepads.Count > 0 ? "Gamepad Mode" : "Keyboard Mode");
        }
    }

    private void HandleGamepadInput(Gamepad gamepad)
    {
        // Face Buttons
        bool aPressed = gamepad.AButton;
        bool bPressed = gamepad.BButton;
        if ((aPressed && !_lastButtonAState) || (bPressed && !_lastButtonBState))
        {
            ToggleStartStop();
        }
        _lastButtonAState = aPressed;
        _lastButtonBState = bPressed;

        bool xPressed = gamepad.XButton;
        if (xPressed && !_lastButtonXState)
        {
            _isReversing = !_isReversing;
        }
        _lastButtonXState = xPressed;

        bool yPressed = gamepad.YButton;
        if (yPressed && !_lastButtonYState)
        {
            FadeCheckBox.IsChecked = !FadeCheckBox.IsChecked;
        }
        _lastButtonYState = yPressed;

        // DPad
        Direction hat = gamepad.Hat;
        bool upPressed = hat == Direction.North || hat == Direction.NorthEast || hat == Direction.NorthWest;
        bool downPressed = hat == Direction.South || hat == Direction.SouthEast || hat == Direction.SouthWest;
        bool leftPressed = hat == Direction.West || hat == Direction.NorthWest || hat == Direction.SouthWest;
        bool rightPressed = hat == Direction.East || hat == Direction.NorthEast || hat == Direction.SouthEast;

        if (upPressed && !_lastDPadUpState)
        {
            int step = (DateTime.Now - _lastDPadUpTime).TotalMilliseconds < 400 ? 10 : 1;
            _lastDPadUpTime = DateTime.Now;
            AdjustFontSize(step);
        }
        _lastDPadUpState = upPressed;

        if (downPressed && !_lastDPadDownState)
        {
            int step = (DateTime.Now - _lastDPadDownTime).TotalMilliseconds < 400 ? -10 : -1;
            _lastDPadDownTime = DateTime.Now;
            AdjustFontSize(step);
        }
        _lastDPadDownState = downPressed;

        if (leftPressed && !_lastDPadLeftState)
        {
            _isReversing = true;
        }
        _lastDPadLeftState = leftPressed;

        if (rightPressed && !_lastDPadRightState)
        {
            _isReversing = false;
        }
        _lastDPadRightState = rightPressed;

        // Shoulders
        bool lbPressed = gamepad.LeftBumper;
        if (lbPressed && !_lastLBState)
        {
            SpeedSlider.Value -= 50;
        }
        _lastLBState = lbPressed;

        bool rbPressed = gamepad.RightBumper;
        if (rbPressed && !_lastRBState)
        {
            SpeedSlider.Value += 50;
        }
        _lastRBState = rbPressed;

        // Analog Triggers
        double lt = gamepad.LeftTrigger;
        double rt = gamepad.RightTrigger;

        bool ltIsDown = lt > 0.1;
        if (ltIsDown && !_ltWasDown)
        {
            if ((DateTime.Now - _lastLTPressTime).TotalMilliseconds < 400)
                _ltBoosted = true;
            else
                _ltBoosted = false;
            _lastLTPressTime = DateTime.Now;
        }
        if (!ltIsDown) _ltBoosted = false;
        _ltWasDown = ltIsDown;

        bool rtIsDown = rt > 0.1;
        if (rtIsDown && !_rtWasDown)
        {
            if ((DateTime.Now - _lastRTPressTime).TotalMilliseconds < 400)
                _rtBoosted = true;
            else
                _rtBoosted = false;
            _lastRTPressTime = DateTime.Now;
        }
        if (!rtIsDown) _rtBoosted = false;
        _rtWasDown = rtIsDown;

        if (rtIsDown)
        {
            int multiplier = _rtBoosted ? 4 : 1;
            int moveAmount = (int)((rt - 0.1) * 100 * multiplier);
            if (moveAmount > 0)
            {
                _currentPosition = Math.Min(_fullText.Length, _currentPosition + moveAmount);
                _currentOffsetX = 0;
                UpdateDisplayedText();
            }
        }
        else if (ltIsDown)
        {
            int multiplier = _ltBoosted ? 4 : 1;
            int moveAmount = (int)((lt - 0.1) * 100 * multiplier);
            if (moveAmount > 0)
            {
                _currentPosition = Math.Max(0, _currentPosition - moveAmount);
                _currentOffsetX = 0;
                UpdateDisplayedText();
            }
        }
    }
}
