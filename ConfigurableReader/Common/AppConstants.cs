namespace ConfigurableReader.Common;

public static class AppConstants
{
    // Rendering & Scrolling
    public const int TimerIntervalMs = 20;
    public const int BufferUpdateThreshold = 5000;
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
}
