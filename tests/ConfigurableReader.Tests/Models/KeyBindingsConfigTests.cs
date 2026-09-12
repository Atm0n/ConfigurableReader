using Avalonia.Input;
using ConfigurableReader.Models;
using Shouldly;
using System;
using System.Text.Json;
using Xunit;

namespace ConfigurableReader.Tests;

public class KeyBindingsConfigTests
{
    [Fact]
    public void GetDefaultBindings_ContainsAllReaderActions()
    {
        var config = KeyBindingsConfig.GetDefaultBindings();

        foreach (var action in Enum.GetValues<ReaderAction>())
        {
            config.Bindings.ContainsKey(action.ToString()).ShouldBeTrue();
            var key = config.GetKey(action);
            key.ShouldNotBe(Key.None);
        }
    }

    [Fact]
    public void GetDefaultKey_ReturnsExpectedDefaults()
    {
        KeyBindingsConfig.GetDefaultKey(ReaderAction.TogglePlayPause).ShouldBe(Key.Space);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.DirectionBackward).ShouldBe(Key.Left);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.DirectionForward).ShouldBe(Key.Right);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.ToggleReverse).ShouldBe(Key.R);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.FontSizeIncrease).ShouldBe(Key.Up);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.FontSizeDecrease).ShouldBe(Key.Down);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.ToggleEdgeFade).ShouldBe(Key.F);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.ToggleSettings).ShouldBe(Key.S);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.CycleTheme).ShouldBe(Key.T);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.ToggleReadingMode).ShouldBe(Key.M);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.OpenWebpage).ShouldBe(Key.U);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.ToggleZenMode).ShouldBe(Key.F11);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.SpeedIncrease).ShouldBe(Key.OemPlus);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.SpeedDecrease).ShouldBe(Key.OemMinus);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.ShowInfo).ShouldBe(Key.I);
        KeyBindingsConfig.GetDefaultKey(ReaderAction.ConfigureKeybindings).ShouldBe(Key.K);
    }

    [Fact]
    public void SetKey_And_GetActionForKey_WorkCorrectly()
    {
        var config = new KeyBindingsConfig();
        config.SetKey(ReaderAction.TogglePlayPause, Key.P);

        config.GetKey(ReaderAction.TogglePlayPause).ShouldBe(Key.P);
        config.GetActionForKey(Key.P).ShouldBe(ReaderAction.TogglePlayPause);
    }

    [Fact]
    public void GetActionForKey_ResolvesSecondaryNumpadAliases()
    {
        var config = KeyBindingsConfig.GetDefaultBindings();

        config.GetActionForKey(Key.Add).ShouldBe(ReaderAction.SpeedIncrease);
        config.GetActionForKey(Key.Subtract).ShouldBe(ReaderAction.SpeedDecrease);
    }

    [Fact]
    public void ResetToDefaults_RestoresOriginalBindings()
    {
        var config = KeyBindingsConfig.GetDefaultBindings();
        config.SetKey(ReaderAction.TogglePlayPause, Key.X);
        config.GetKey(ReaderAction.TogglePlayPause).ShouldBe(Key.X);

        config.ResetToDefaults();
        config.GetKey(ReaderAction.TogglePlayPause).ShouldBe(Key.Space);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = KeyBindingsConfig.GetDefaultBindings();
        var clone = original.Clone();

        clone.SetKey(ReaderAction.TogglePlayPause, Key.Z);
        original.GetKey(ReaderAction.TogglePlayPause).ShouldBe(Key.Space);
        clone.GetKey(ReaderAction.TogglePlayPause).ShouldBe(Key.Z);
    }

    [Fact]
    public void Serialization_RoundtripPreservesBindings()
    {
        var config = KeyBindingsConfig.GetDefaultBindings();
        config.SetKey(ReaderAction.TogglePlayPause, Key.Enter);
        config.SetKey(ReaderAction.ToggleZenMode, Key.F12);

        string json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<KeyBindingsConfig>(json);

        deserialized.ShouldNotBeNull();
        deserialized.GetKey(ReaderAction.TogglePlayPause).ShouldBe(Key.Enter);
        deserialized.GetKey(ReaderAction.ToggleZenMode).ShouldBe(Key.F12);
    }

    [Fact]
    public void GetKey_FallsBackToDefault_WhenKeyIsMissingOrInvalid()
    {
        var config = new KeyBindingsConfig();
        // Empty dictionary
        config.GetKey(ReaderAction.TogglePlayPause).ShouldBe(Key.Space);

        // Invalid key name in dictionary
        config.Bindings[nameof(ReaderAction.TogglePlayPause)] = "NonExistentKey123";
        config.GetKey(ReaderAction.TogglePlayPause).ShouldBe(Key.Space);
    }
}
