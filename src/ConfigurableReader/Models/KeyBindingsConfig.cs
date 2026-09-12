using Avalonia.Input;
using System;
using System.Collections.Generic;

namespace ConfigurableReader.Models;

public enum ReaderAction
{
    TogglePlayPause,
    DirectionBackward,
    DirectionForward,
    ToggleReverse,
    FontSizeIncrease,
    FontSizeDecrease,
    ToggleEdgeFade,
    ToggleSettings,
    CycleTheme,
    ToggleReadingMode,
    OpenWebpage,
    ToggleZenMode,
    SpeedIncrease,
    SpeedDecrease,
    ShowInfo,
    ConfigureKeybindings,
}

public class KeyBindingsConfig
{
    public Dictionary<string, string> Bindings { get; set; } = new();

    public static Key GetDefaultKey(ReaderAction action) => action switch
    {
        ReaderAction.TogglePlayPause => Key.Space,
        ReaderAction.DirectionBackward => Key.Left,
        ReaderAction.DirectionForward => Key.Right,
        ReaderAction.ToggleReverse => Key.R,
        ReaderAction.FontSizeIncrease => Key.Up,
        ReaderAction.FontSizeDecrease => Key.Down,
        ReaderAction.ToggleEdgeFade => Key.F,
        ReaderAction.ToggleSettings => Key.S,
        ReaderAction.CycleTheme => Key.T,
        ReaderAction.ToggleReadingMode => Key.M,
        ReaderAction.OpenWebpage => Key.U,
        ReaderAction.ToggleZenMode => Key.F11,
        ReaderAction.SpeedIncrease => Key.OemPlus,
        ReaderAction.SpeedDecrease => Key.OemMinus,
        ReaderAction.ShowInfo => Key.I,
        ReaderAction.ConfigureKeybindings => Key.K,
        _ => Key.None
    };

    public static KeyBindingsConfig GetDefaultBindings()
    {
        var config = new KeyBindingsConfig();
        foreach (var action in Enum.GetValues<ReaderAction>())
        {
            config.Bindings[action.ToString()] = GetDefaultKey(action).ToString();
        }
        return config;
    }

    public Key GetKey(ReaderAction action)
    {
        if (Bindings.TryGetValue(action.ToString(), out var keyStr) &&
            Enum.TryParse<Key>(keyStr, true, out var key))
        {
            return key;
        }
        return GetDefaultKey(action);
    }

    public void SetKey(ReaderAction action, Key key)
    {
        Bindings[action.ToString()] = key.ToString();
    }

    public ReaderAction? GetActionForKey(Key key)
    {
        foreach (var action in Enum.GetValues<ReaderAction>())
        {
            if (GetKey(action) == key)
            {
                return action;
            }
        }

        // Secondary aliases for keyboard numpad
        if (key == Key.Add && GetKey(ReaderAction.SpeedIncrease) == Key.OemPlus)
            return ReaderAction.SpeedIncrease;
        if (key == Key.Subtract && GetKey(ReaderAction.SpeedDecrease) == Key.OemMinus)
            return ReaderAction.SpeedDecrease;

        return null;
    }

    public void ResetToDefaults()
    {
        Bindings.Clear();
        foreach (var action in Enum.GetValues<ReaderAction>())
        {
            Bindings[action.ToString()] = GetDefaultKey(action).ToString();
        }
    }

    public KeyBindingsConfig Clone()
    {
        return new KeyBindingsConfig
        {
            Bindings = new Dictionary<string, string>(Bindings)
        };
    }
}
