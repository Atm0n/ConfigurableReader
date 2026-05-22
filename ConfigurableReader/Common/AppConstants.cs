using System.Collections.Generic;

namespace ConfigurableReader.Common;

public static class AppConstants
{
    // Rendering & Scrolling
    public const int TimerIntervalMs = 20;
    public const int MaxBufferLength = 10000;
    public const double VerticalCenteringMultiplier = 1.5;
    
    // UI & Logic
    public const int DoubleTapThresholdMs = 400;
    public const int DefaultSpeedIncrement = 50;
    public const int SmallFontSizeStep = 1;
    public const int LargeFontSizeStep = 10;
    
    // Gamepad
    public const double GamepadTriggerThreshold = 0.1;
    public const int GamepadBoostMultiplier = 4;
    public const int GamepadBaseMoveAmount = 100;
    
    // Constraints
    public const double MinFontSize = 10;
    public const double MaxFontSize = 800;
    public const double MaxScrollSpeed = 1000;

    /// <summary>
    /// The languages the app supports.
    /// Code is the BCP-47 locale tag (matches the /Localization/*.axaml filename).
    /// DisplayName is what appears in the UI combo box.
    /// Adding a new translation only requires adding an entry here and the matching .axaml file.
    /// </summary>
    public static readonly IReadOnlyList<(string Code, string DisplayName)> SupportedLanguages =
    [
        ("en-US", "English"),
        ("es-ES", "Español"),
        ("ca-ES", "Català"),
    ];
}
