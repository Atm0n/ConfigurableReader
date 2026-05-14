using System;
using System.Collections.Generic;
using Avalonia.Threading;
using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using DevDecoder.HIDDevices.Converters;

namespace ConfigurableReader.Services;

public class GamepadService : IDisposable
{
    private readonly Devices _devices = new();
    private IDisposable? _gamepadSubscription;
    private readonly HashSet<Gamepad> _activeGamepads = [];

    // Events for high-level actions
    public event Action? ToggleStartStopRequested;
    public event Action? ToggleReverseRequested;
    public event Action? ToggleFadeRequested;
    public event Action? ToggleSettingsRequested;
    public event Action? ShowInfoRequested;
    public event Action<int>? FontSizeAdjustmentRequested;
    public event Action<double>? SpeedAdjustmentRequested;
    public event Action<int>? PositionAdjustmentRequested;
    public event Action<bool>? SetReverseDirectionRequested;
    public event Action<bool>? InputModeChanged;

    // State for input detection
    private bool _lastButtonAState = false;
    private bool _lastButtonBState = false;
    private bool _lastButtonXState = false;
    private bool _lastButtonYState = false;
    private bool _lastButtonStartState = false;
    private bool _lastButtonBackState = false;
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

    public bool HasActiveGamepads => _activeGamepads.Count > 0;

    public void Start()
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
                    InputModeChanged?.Invoke(HasActiveGamepads);
                });
            });

            gamepad.Changes.Subscribe(_ =>
            {
                Dispatcher.UIThread.Post(() => HandleGamepadInput(gamepad));
            });
        });
    }

    private void HandleGamepadInput(Gamepad gamepad)
    {
        // Face Buttons
        bool aPressed = gamepad.AButton;
        bool bPressed = gamepad.BButton;
        if ((aPressed && !_lastButtonAState) || (bPressed && !_lastButtonBState))
        {
            ToggleStartStopRequested?.Invoke();
        }
        _lastButtonAState = aPressed;
        _lastButtonBState = bPressed;

        bool xPressed = gamepad.XButton;
        if (xPressed && !_lastButtonXState)
        {
            ToggleReverseRequested?.Invoke();
        }
        _lastButtonXState = xPressed;

        bool yPressed = gamepad.YButton;
        if (yPressed && !_lastButtonYState)
        {
            ToggleFadeRequested?.Invoke();
        }
        _lastButtonYState = yPressed;

        // Menu Buttons
        bool startPressed = gamepad.Start;
        if (startPressed && !_lastButtonStartState)
        {
            ToggleSettingsRequested?.Invoke();
        }
        _lastButtonStartState = startPressed;

        bool backPressed = gamepad.Select;
        if (backPressed && !_lastButtonBackState)
        {
            ShowInfoRequested?.Invoke();
        }
        _lastButtonBackState = backPressed;

        // DPad
        Direction hat = gamepad.Hat;
        bool upPressed = hat == Direction.North || hat == Direction.NorthEast || hat == Direction.NorthWest;
        bool downPressed = hat == Direction.South || hat == Direction.SouthEast || hat == Direction.SouthWest;
        bool leftPressed = hat == Direction.West || hat == Direction.NorthWest || hat == Direction.SouthWest;
        bool rightPressed = hat == Direction.East || hat == Direction.NorthEast || hat == Direction.SouthEast;

        if (upPressed && !_lastDPadUpState)
        {
            int step = (DateTime.Now - _lastDPadUpTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs 
                ? AppConstants.LargeFontSizeStep 
                : AppConstants.SmallFontSizeStep;
            _lastDPadUpTime = DateTime.Now;
            FontSizeAdjustmentRequested?.Invoke(step);
        }
        _lastDPadUpState = upPressed;

        if (downPressed && !_lastDPadDownState)
        {
            int step = (DateTime.Now - _lastDPadDownTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs 
                ? -AppConstants.LargeFontSizeStep 
                : -AppConstants.SmallFontSizeStep;
            _lastDPadDownTime = DateTime.Now;
            FontSizeAdjustmentRequested?.Invoke(step);
        }
        _lastDPadDownState = downPressed;

        if (leftPressed && !_lastDPadLeftState)
        {
            SetReverseDirectionRequested?.Invoke(true);
        }
        _lastDPadLeftState = leftPressed;

        if (rightPressed && !_lastDPadRightState)
        {
            SetReverseDirectionRequested?.Invoke(false);
        }
        _lastDPadRightState = rightPressed;

        // Shoulders
        bool lbPressed = gamepad.LeftBumper;
        if (lbPressed && !_lastLBState)
        {
            SpeedAdjustmentRequested?.Invoke(-AppConstants.DefaultSpeedIncrement);
        }
        _lastLBState = lbPressed;

        bool rbPressed = gamepad.RightBumper;
        if (rbPressed && !_lastRBState)
        {
            SpeedAdjustmentRequested?.Invoke(AppConstants.DefaultSpeedIncrement);
        }
        _lastRBState = rbPressed;

        // Analog Triggers
        double lt = gamepad.LeftTrigger;
        double rt = gamepad.RightTrigger;

        bool ltIsDown = lt > AppConstants.GamepadTriggerThreshold;
        if (ltIsDown && !_ltWasDown)
        {
            if ((DateTime.Now - _lastLTPressTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs)
                _ltBoosted = true;
            else
                _ltBoosted = false;
            _lastLTPressTime = DateTime.Now;
        }
        if (!ltIsDown) _ltBoosted = false;
        _ltWasDown = ltIsDown;

        bool rtIsDown = rt > AppConstants.GamepadTriggerThreshold;
        if (rtIsDown && !_rtWasDown)
        {
            if ((DateTime.Now - _lastRTPressTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs)
                _rtBoosted = true;
            else
                _rtBoosted = false;
            _lastRTPressTime = DateTime.Now;
        }
        if (!rtIsDown) _rtBoosted = false;
        _rtWasDown = rtIsDown;

        if (rtIsDown)
        {
            int multiplier = _rtBoosted ? AppConstants.GamepadBoostMultiplier : 1;
            int moveAmount = (int)((rt - AppConstants.GamepadTriggerThreshold) * AppConstants.GamepadBaseMoveAmount * multiplier);
            if (moveAmount > 0)
            {
                PositionAdjustmentRequested?.Invoke(moveAmount);
            }
        }
        else if (ltIsDown)
        {
            int multiplier = _ltBoosted ? AppConstants.GamepadBoostMultiplier : 1;
            int moveAmount = (int)((lt - AppConstants.GamepadTriggerThreshold) * AppConstants.GamepadBaseMoveAmount * multiplier);
            if (moveAmount > 0)
            {
                PositionAdjustmentRequested?.Invoke(-moveAmount);
            }
        }
    }

    public void Dispose()
    {
        _gamepadSubscription?.Dispose();
        _devices.Dispose();
        GC.SuppressFinalize(this);
    }
}
