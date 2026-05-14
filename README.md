# ConfigurableReader

A highly customizable, cross-platform text reader designed for a comfortable and hands-free reading experience. Built with [Avalonia UI](https://github.com/AvaloniaUI/Avalonia).

## Features

- **Auto-Scrolling:** Adjust scroll speed to match your reading pace.
- **Visual Customization:**
  - Change font size (supports massive fonts for accessibility).
  - Customize text and background colors.
  - Edge fading for better focus.
- **Progress Tracking:** Automatically saves your last position in each book you read.
- **Gamepad Support:** Control your reading experience from the comfort of your couch using any standard HID Gamepad (Xbox, PlayStation, Switch, etc.).
- **Visual Indicators:** Connection indicator shows whether you are in Keyboard (⌨️) or Gamepad (🎮) mode.
- **Keyboard Shortcuts:** Full control with your keyboard.

## Controls

### Keyboard
- **Space:** Play / Pause
- **Left / Right Arrows:** Set Direction (Forward/Backward)
- **Up / Down Arrows:** Adjust Font Size (Tap for 1pt, double-tap for 10pt)
- **R:** Toggle Direction
- **F:** Toggle Edge Fade
- **S:** Toggle Settings Panel
- **I:** Show Info / About
- **+/- (Numpad or Main):** Adjust Scroll Speed

### Gamepad
- **A / B Buttons:** Play / Pause
- **DPad Up / Down:** Adjust Font Size
- **DPad Left / Right:** Set Direction (Forward/Backward)
- **X Button:** Toggle Direction
- **Y Button:** Toggle Edge Fade
- **LB / RB (Bumpers):** Adjust Scroll Speed
- **LT / RT (Triggers):** Rewind / Fast-Forward (Hold to scroll, double-tap for Boost)
- **Input Indicator:** Check the bottom-right corner for ⌨️/🎮 icon.

## Getting Started

1. Launch the application.
2. Click **Open File** to select a `.txt` book.
3. Use the **Speed Slider** (or controller bumpers) to adjust how fast the text scrolls.
4. Click **Start** (or press Space / A / B) to begin reading.

## Dependencies

This project uses the following third-party libraries:

- [Avalonia](https://github.com/AvaloniaUI/Avalonia) (MIT License)
- [HIDDevices](https://github.com/DevDecoder/HIDDevices) (Apache License 2.0)
- [HIDDevices.Usages](https://github.com/DevDecoder/HIDDevices) (Apache License 2.0)
- [HIDSharp](https://github.com/zerog_dog/HIDSharp) (Apache License 2.0)

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
