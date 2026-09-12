using Avalonia.Threading;
using ConfigurableReader.Common;
using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using DevDecoder.HIDDevices.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ConfigurableReader.Services;

public readonly record struct GamepadInputSnapshot(
    bool AButton,
    bool BButton,
    bool XButton,
    bool YButton,
    bool Start,
    bool Select,
    bool LeftBumper,
    bool RightBumper,
    Direction Hat,
    double LeftTrigger,
    double RightTrigger
);

public class GamepadDeviceState
{
    public bool AButton { get; set; }
    public bool BButton { get; set; }
    public bool XButton { get; set; }
    public bool YButton { get; set; }
    public bool Start { get; set; }
    public bool Select { get; set; }
    public bool LeftBumper { get; set; }
    public bool RightBumper { get; set; }
    public bool DPadUp { get; set; }
    public bool DPadDown { get; set; }
    public bool DPadLeft { get; set; }
    public bool DPadRight { get; set; }

    public DateTime LastAButtonTime { get; set; } = DateTime.MinValue;
    public DateTime LastBButtonTime { get; set; } = DateTime.MinValue;
    public DateTime LastXButtonTime { get; set; } = DateTime.MinValue;
    public DateTime LastYButtonTime { get; set; } = DateTime.MinValue;
    public DateTime LastStartTime { get; set; } = DateTime.MinValue;
    public DateTime LastSelectTime { get; set; } = DateTime.MinValue;
    public DateTime LastLeftBumperTime { get; set; } = DateTime.MinValue;
    public DateTime LastRightBumperTime { get; set; } = DateTime.MinValue;
    public DateTime LastDPadUpTime { get; set; } = DateTime.MinValue;
    public DateTime LastDPadDownTime { get; set; } = DateTime.MinValue;
    public DateTime LastDPadLeftTime { get; set; } = DateTime.MinValue;
    public DateTime LastDPadRightTime { get; set; } = DateTime.MinValue;

    public bool LtWasDown { get; set; }
    public bool RtWasDown { get; set; }
    public bool LtBoosted { get; set; }
    public bool RtBoosted { get; set; }
    public DateTime LastLtPressTime { get; set; } = DateTime.MinValue;
    public DateTime LastRtPressTime { get; set; } = DateTime.MinValue;

    public double CurrentLt { get; set; }
    public double CurrentRt { get; set; }
}

public class GamepadService : IDisposable
{
    private readonly Devices _devices = new();
    private IDisposable? _gamepadSubscription;
    private readonly HashSet<Gamepad> _activeGamepads = [];
    private readonly ConcurrentDictionary<Gamepad, GamepadDeviceState> _gamepadStates = new();
    private readonly ConcurrentDictionary<Gamepad, List<IDisposable>> _gamepadSubscriptions = new();

    private readonly Timer _triggerTimer;
    private bool _isTriggerTimerRunning = false;

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

    public Func<bool>? IsActive { get; set; }

    public bool HasActiveGamepads => _activeGamepads.Count > 0;

    public GamepadService()
    {
        _triggerTimer = new Timer(OnTriggerTimerTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        _gamepadSubscription = _devices.Controllers<Gamepad>().Subscribe(gamepad =>
        {
            gamepad.Connect();

            var subs = new List<IDisposable>();

            subs.Add(gamepad.ConnectionState.Subscribe(isConnected =>
            {
                DispatchToUI(() =>
                {
                    if (isConnected)
                    {
                        _activeGamepads.Add(gamepad);
                    }
                    else
                    {
                        _activeGamepads.Remove(gamepad);
                        _gamepadStates.TryRemove(gamepad, out _);
                        UpdateTriggerTimerState();
                    }
                    InputModeChanged?.Invoke(HasActiveGamepads);
                });
            }));

            var state = _gamepadStates.GetOrAdd(gamepad, _ => new GamepadDeviceState());

            subs.Add(gamepad.Changes.Subscribe(changes =>
            {
                var snapshot = new GamepadInputSnapshot(
                    gamepad.AButton,
                    gamepad.BButton,
                    gamepad.XButton,
                    gamepad.YButton,
                    gamepad.Start,
                    gamepad.Select,
                    gamepad.LeftBumper,
                    gamepad.RightBumper,
                    gamepad.Hat,
                    gamepad.LeftTrigger,
                    gamepad.RightTrigger
                );

                ProcessInput(state, snapshot, changes);
            }));

            _gamepadSubscriptions[gamepad] = subs;
        });
    }

    public void ProcessInput(
        GamepadDeviceState state,
        GamepadInputSnapshot snapshot,
        IList<ControlValue>? changes = null,
        Action<Action>? customDispatcher = null)
    {
        var dispatch = customDispatcher ?? DispatchToUI;
        var now = DateTime.UtcNow;

        bool aPressed = snapshot.AButton;
        bool bPressed = snapshot.BButton;
        bool xPressed = snapshot.XButton;
        bool yPressed = snapshot.YButton;
        bool startPressed = snapshot.Start;
        bool selectPressed = snapshot.Select;
        bool lbPressed = snapshot.LeftBumper;
        bool rbPressed = snapshot.RightBumper;

        if (changes != null)
        {
            foreach (var change in changes)
            {
                if (change.Value is true)
                {
                    if (change.PropertyName == nameof(snapshot.AButton)) aPressed = true;
                    else if (change.PropertyName == nameof(snapshot.BButton)) bPressed = true;
                    else if (change.PropertyName == nameof(snapshot.XButton)) xPressed = true;
                    else if (change.PropertyName == nameof(snapshot.YButton)) yPressed = true;
                    else if (change.PropertyName == nameof(snapshot.Start)) startPressed = true;
                    else if (change.PropertyName == nameof(snapshot.Select)) selectPressed = true;
                    else if (change.PropertyName == nameof(snapshot.LeftBumper)) lbPressed = true;
                    else if (change.PropertyName == nameof(snapshot.RightBumper)) rbPressed = true;
                }
            }
        }

        lock (state)
        {
            // Face button: A
            if (aPressed && !state.AButton)
            {
                if ((now - state.LastAButtonTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastAButtonTime = now;
                    dispatch(() => ToggleStartStopRequested?.Invoke());
                }
            }
            state.AButton = snapshot.AButton;

            // Face button: B
            if (bPressed && !state.BButton)
            {
                if ((now - state.LastBButtonTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastBButtonTime = now;
                    dispatch(() => ToggleStartStopRequested?.Invoke());
                }
            }
            state.BButton = snapshot.BButton;

            // Face button: X (Reverse)
            if (xPressed && !state.XButton)
            {
                if ((now - state.LastXButtonTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastXButtonTime = now;
                    dispatch(() => ToggleReverseRequested?.Invoke());
                }
            }
            state.XButton = snapshot.XButton;

            // Face button: Y (Edge fade)
            if (yPressed && !state.YButton)
            {
                if ((now - state.LastYButtonTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastYButtonTime = now;
                    dispatch(() => ToggleFadeRequested?.Invoke());
                }
            }
            state.YButton = snapshot.YButton;

            // Menu button: Start (Settings)
            if (startPressed && !state.Start)
            {
                if ((now - state.LastStartTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastStartTime = now;
                    dispatch(() => ToggleSettingsRequested?.Invoke());
                }
            }
            state.Start = snapshot.Start;

            // Menu button: Select (Info)
            if (selectPressed && !state.Select)
            {
                if ((now - state.LastSelectTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastSelectTime = now;
                    dispatch(() => ShowInfoRequested?.Invoke());
                }
            }
            state.Select = snapshot.Select;

            // Bumper: LB (Speed down)
            if (lbPressed && !state.LeftBumper)
            {
                if ((now - state.LastLeftBumperTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastLeftBumperTime = now;
                    dispatch(() => SpeedAdjustmentRequested?.Invoke(-AppConstants.DefaultSpeedIncrement));
                }
            }
            state.LeftBumper = snapshot.LeftBumper;

            // Bumper: RB (Speed up)
            if (rbPressed && !state.RightBumper)
            {
                if ((now - state.LastRightBumperTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastRightBumperTime = now;
                    dispatch(() => SpeedAdjustmentRequested?.Invoke(AppConstants.DefaultSpeedIncrement));
                }
            }
            state.RightBumper = snapshot.RightBumper;

            // D-Pad
            Direction hat = snapshot.Hat;
            bool upPressed = hat is Direction.North or Direction.NorthEast or Direction.NorthWest;
            bool downPressed = hat is Direction.South or Direction.SouthEast or Direction.SouthWest;
            bool leftPressed = hat is Direction.West or Direction.NorthWest or Direction.SouthWest;
            bool rightPressed = hat is Direction.East or Direction.NorthEast or Direction.SouthEast;

            if (upPressed && !state.DPadUp)
            {
                double timeSinceLast = (now - state.LastDPadUpTime).TotalMilliseconds;
                if (timeSinceLast >= AppConstants.GamepadButtonDebounceMs)
                {
                    int step = timeSinceLast < AppConstants.DoubleTapThresholdMs
                        ? AppConstants.LargeFontSizeStep
                        : AppConstants.SmallFontSizeStep;
                    state.LastDPadUpTime = now;
                    dispatch(() => FontSizeAdjustmentRequested?.Invoke(step));
                }
            }
            state.DPadUp = upPressed;

            if (downPressed && !state.DPadDown)
            {
                double timeSinceLast = (now - state.LastDPadDownTime).TotalMilliseconds;
                if (timeSinceLast >= AppConstants.GamepadButtonDebounceMs)
                {
                    int step = timeSinceLast < AppConstants.DoubleTapThresholdMs
                        ? -AppConstants.LargeFontSizeStep
                        : -AppConstants.SmallFontSizeStep;
                    state.LastDPadDownTime = now;
                    dispatch(() => FontSizeAdjustmentRequested?.Invoke(step));
                }
            }
            state.DPadDown = downPressed;

            if (leftPressed && !state.DPadLeft)
            {
                if ((now - state.LastDPadLeftTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastDPadLeftTime = now;
                    dispatch(() => SetReverseDirectionRequested?.Invoke(true));
                }
            }
            state.DPadLeft = leftPressed;

            if (rightPressed && !state.DPadRight)
            {
                if ((now - state.LastDPadRightTime).TotalMilliseconds >= AppConstants.GamepadButtonDebounceMs)
                {
                    state.LastDPadRightTime = now;
                    dispatch(() => SetReverseDirectionRequested?.Invoke(false));
                }
            }
            state.DPadRight = rightPressed;

            // Triggers
            double lt = snapshot.LeftTrigger;
            double rt = snapshot.RightTrigger;
            state.CurrentLt = lt;
            state.CurrentRt = rt;

            bool ltIsDown = lt > AppConstants.GamepadTriggerThreshold;
            if (ltIsDown && !state.LtWasDown)
            {
                state.LtBoosted = (now - state.LastLtPressTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs;
                state.LastLtPressTime = now;
            }
            if (!ltIsDown) state.LtBoosted = false;
            state.LtWasDown = ltIsDown;

            bool rtIsDown = rt > AppConstants.GamepadTriggerThreshold;
            if (rtIsDown && !state.RtWasDown)
            {
                state.RtBoosted = (now - state.LastRtPressTime).TotalMilliseconds < AppConstants.DoubleTapThresholdMs;
                state.LastRtPressTime = now;
            }
            if (!rtIsDown) state.RtBoosted = false;
            state.RtWasDown = rtIsDown;

            UpdateTriggerTimerState();
        }
    }

    private void UpdateTriggerTimerState()
    {
        bool anyTriggerActive = false;
        foreach (var s in _gamepadStates.Values)
        {
            if (s.RtWasDown || s.LtWasDown)
            {
                anyTriggerActive = true;
                break;
            }
        }

        if (anyTriggerActive && !_isTriggerTimerRunning)
        {
            _isTriggerTimerRunning = true;
            _triggerTimer.Change(0, 40);
        }
        else if (!anyTriggerActive && _isTriggerTimerRunning)
        {
            _isTriggerTimerRunning = false;
            _triggerTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void OnTriggerTimerTick(object? _)
    {
        foreach (var s in _gamepadStates.Values)
        {
            lock (s)
            {
                if (s.RtWasDown)
                {
                    int multiplier = s.RtBoosted ? AppConstants.GamepadBoostMultiplier : 1;
                    int moveAmount = (int)((s.CurrentRt - AppConstants.GamepadTriggerThreshold) * AppConstants.GamepadBaseMoveAmount * multiplier);
                    if (moveAmount > 0)
                    {
                        DispatchToUI(() => PositionAdjustmentRequested?.Invoke(moveAmount));
                    }
                }
                else if (s.LtWasDown)
                {
                    int multiplier = s.LtBoosted ? AppConstants.GamepadBoostMultiplier : 1;
                    int moveAmount = (int)((s.CurrentLt - AppConstants.GamepadTriggerThreshold) * AppConstants.GamepadBaseMoveAmount * multiplier);
                    if (moveAmount > 0)
                    {
                        DispatchToUI(() => PositionAdjustmentRequested?.Invoke(-moveAmount));
                    }
                }
            }
        }
    }

    private void DispatchToUI(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            if (IsActive == null || IsActive())
            {
                action();
            }
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (IsActive == null || IsActive())
                {
                    action();
                }
            });
        }
    }

    public void Dispose()
    {
        _triggerTimer.Dispose();
        _gamepadSubscription?.Dispose();
        foreach (var list in _gamepadSubscriptions.Values)
        {
            foreach (var sub in list)
            {
                sub.Dispose();
            }
        }
        _gamepadSubscriptions.Clear();
        _devices.Dispose();
        GC.SuppressFinalize(this);
    }
}
