using ConfigurableReader.Common;
using ConfigurableReader.Services;
using DevDecoder.HIDDevices.Converters;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ConfigurableReader.Tests.Services;

public class GamepadServiceTests
{
    private static void ExecuteSynchronously(Action action) => action();

    [Fact]
    public void ButtonA_PressDown_TriggersToggleStartStop()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int triggeredCount = 0;
        service.ToggleStartStopRequested += () => triggeredCount++;

        var snapshotDown = new GamepadInputSnapshot { AButton = true };
        service.ProcessInput(state, snapshotDown, customDispatcher: ExecuteSynchronously);

        triggeredCount.ShouldBe(1);
        state.AButton.ShouldBeTrue();

        // Holding button down should NOT trigger again
        service.ProcessInput(state, snapshotDown, customDispatcher: ExecuteSynchronously);
        triggeredCount.ShouldBe(1);

        // Releasing button
        var snapshotUp = new GamepadInputSnapshot { AButton = false };
        service.ProcessInput(state, snapshotUp, customDispatcher: ExecuteSynchronously);
        triggeredCount.ShouldBe(1);
        state.AButton.ShouldBeFalse();
    }

    [Fact]
    public void ButtonB_PressDown_TriggersToggleStartStop()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int triggeredCount = 0;
        service.ToggleStartStopRequested += () => triggeredCount++;

        var snapshotDown = new GamepadInputSnapshot { BButton = true };
        service.ProcessInput(state, snapshotDown, customDispatcher: ExecuteSynchronously);

        triggeredCount.ShouldBe(1);
        state.BButton.ShouldBeTrue();
    }

    [Fact]
    public void ButtonX_PressDown_TriggersToggleReverse()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int triggeredCount = 0;
        service.ToggleReverseRequested += () => triggeredCount++;

        var snapshot = new GamepadInputSnapshot { XButton = true };
        service.ProcessInput(state, snapshot, customDispatcher: ExecuteSynchronously);

        triggeredCount.ShouldBe(1);
        state.XButton.ShouldBeTrue();
    }

    [Fact]
    public void ButtonY_PressDown_TriggersToggleFade()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int triggeredCount = 0;
        service.ToggleFadeRequested += () => triggeredCount++;

        var snapshot = new GamepadInputSnapshot { YButton = true };
        service.ProcessInput(state, snapshot, customDispatcher: ExecuteSynchronously);

        triggeredCount.ShouldBe(1);
        state.YButton.ShouldBeTrue();
    }

    [Fact]
    public void MenuButtons_StartAndSelect_TriggerExpectedEvents()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int startCount = 0;
        int selectCount = 0;
        service.ToggleSettingsRequested += () => startCount++;
        service.ShowInfoRequested += () => selectCount++;

        service.ProcessInput(state, new GamepadInputSnapshot { Start = true }, customDispatcher: ExecuteSynchronously);
        startCount.ShouldBe(1);

        service.ProcessInput(state, new GamepadInputSnapshot { Select = true }, customDispatcher: ExecuteSynchronously);
        selectCount.ShouldBe(1);
    }

    [Fact]
    public void Bumpers_LBAndRB_TriggerSpeedAdjustment()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        double totalSpeedDelta = 0;
        service.SpeedAdjustmentRequested += delta => totalSpeedDelta += delta;

        service.ProcessInput(state, new GamepadInputSnapshot { LeftBumper = true }, customDispatcher: ExecuteSynchronously);
        totalSpeedDelta.ShouldBe(-AppConstants.DefaultSpeedIncrement);

        service.ProcessInput(state, new GamepadInputSnapshot { RightBumper = true }, customDispatcher: ExecuteSynchronously);
        totalSpeedDelta.ShouldBe(0);
    }

    [Fact]
    public void DPad_Directions_TriggerDirectionAndFontEvents()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int fontSizeStep = 0;
        bool? reverseDirection = null;

        service.FontSizeAdjustmentRequested += step => fontSizeStep += step;
        service.SetReverseDirectionRequested += rev => reverseDirection = rev;

        // DPad Up (single tap)
        service.ProcessInput(state, new GamepadInputSnapshot { Hat = Direction.North }, customDispatcher: ExecuteSynchronously);
        fontSizeStep.ShouldBe(AppConstants.SmallFontSizeStep);

        // DPad Up released
        service.ProcessInput(state, new GamepadInputSnapshot { Hat = Direction.NotPressed }, customDispatcher: ExecuteSynchronously);

        // DPad Down
        service.ProcessInput(state, new GamepadInputSnapshot { Hat = Direction.South }, customDispatcher: ExecuteSynchronously);
        fontSizeStep.ShouldBe(0); // +1 - 1 = 0

        // DPad Left
        service.ProcessInput(state, new GamepadInputSnapshot { Hat = Direction.West }, customDispatcher: ExecuteSynchronously);
        reverseDirection.ShouldBe(true);

        // DPad Right
        service.ProcessInput(state, new GamepadInputSnapshot { Hat = Direction.East }, customDispatcher: ExecuteSynchronously);
        reverseDirection.ShouldBe(false);
    }

    [Fact]
    public void QuickTapAndRelease_NeverDropsStateTransition()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int count = 0;
        service.ToggleStartStopRequested += () => count++;

        // Quick down and up in sequence
        service.ProcessInput(state, new GamepadInputSnapshot { AButton = true }, customDispatcher: ExecuteSynchronously);
        service.ProcessInput(state, new GamepadInputSnapshot { AButton = false }, customDispatcher: ExecuteSynchronously);

        count.ShouldBe(1);
        state.AButton.ShouldBeFalse();
    }

    [Fact]
    public void InactiveWindow_DoesNotDispatch_ButUpdatesInternalStateToPreventDesync()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int count = 0;
        service.ToggleStartStopRequested += () => count++;

        // Simulated inactive dispatcher (drops invocation)
        static void InactiveDispatcher(Action action) { /* Window inactive, do not invoke */ }

        service.ProcessInput(state, new GamepadInputSnapshot { AButton = true }, customDispatcher: InactiveDispatcher);
        count.ShouldBe(0);
        // Release happens while inactive
        service.ProcessInput(state, new GamepadInputSnapshot { AButton = false }, customDispatcher: InactiveDispatcher);
        state.AButton.ShouldBeFalse(); // State reset cleanly!

        // Reset debounce timer to simulate normal human delay between window switch
        state.LastAButtonTime = DateTime.MinValue;

        // Next press while active MUST succeed!
        service.ProcessInput(state, new GamepadInputSnapshot { AButton = true }, customDispatcher: ExecuteSynchronously);
        count.ShouldBe(1);
    }

    [Fact]
    public void ButtonDebounce_SuppressesHardwareChatter()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();
        int count = 0;
        service.ToggleStartStopRequested += () => count++;

        // First press down
        service.ProcessInput(state, new GamepadInputSnapshot { AButton = true }, customDispatcher: ExecuteSynchronously);
        count.ShouldBe(1);

        // Immediate release (chatter)
        service.ProcessInput(state, new GamepadInputSnapshot { AButton = false }, customDispatcher: ExecuteSynchronously);

        // Immediate re-press within debounce window (< 120ms)
        service.ProcessInput(state, new GamepadInputSnapshot { AButton = true }, customDispatcher: ExecuteSynchronously);
        // Should NOT fire a second time because it's hardware bounce/chatter
        count.ShouldBe(1);
    }

    [Fact]
    public void Triggers_TrackDownAndBoostState()
    {
        using var service = new GamepadService();
        var state = new GamepadDeviceState();

        // Below threshold: not down
        service.ProcessInput(state, new GamepadInputSnapshot { RightTrigger = 0.05 }, customDispatcher: ExecuteSynchronously);
        state.RtWasDown.ShouldBeFalse();
        state.RtBoosted.ShouldBeFalse();

        // Above threshold: down
        service.ProcessInput(state, new GamepadInputSnapshot { RightTrigger = 0.5 }, customDispatcher: ExecuteSynchronously);
        state.RtWasDown.ShouldBeTrue();
        state.RtBoosted.ShouldBeFalse();

        // Release
        service.ProcessInput(state, new GamepadInputSnapshot { RightTrigger = 0.0 }, customDispatcher: ExecuteSynchronously);
        state.RtWasDown.ShouldBeFalse();
        state.RtBoosted.ShouldBeFalse();

        // Quick double-tap triggers boost
        service.ProcessInput(state, new GamepadInputSnapshot { RightTrigger = 0.8 }, customDispatcher: ExecuteSynchronously);
        state.RtWasDown.ShouldBeTrue();
        state.RtBoosted.ShouldBeTrue();
    }
}
