# ConfigurableReader

A highly customizable text reader for Windows (WPF) designed for a comfortable and hands-free reading experience. It supports auto-scrolling, visual customization, and even Xbox controller input.

## Features

- **Auto-Scrolling:** Adjust scroll speed to match your reading pace.
- **Visual Customization:**
  - Change font size.
  - Customize text and background colors.
  - Edge fading for better focus.
- **Progress Tracking:** Automatically saves your last position in each book you read.
- **Xbox Controller Support:** Control your reading experience from the comfort of your couch.
- **Keyboard Shortcuts:** Full control with your keyboard.

## Controls

### Keyboard
- **Space:** Play / Pause
- **Left / Right Arrows:** Set Direction (Forward/Backward)
- **Up / Down Arrows:** Adjust Font Size
- **R:** Toggle Direction
- **F:** Toggle Edge Fade
- **S:** Toggle Settings Panel
- **I:** Show Info / About
- **+/- (Numpad or Main):** Adjust Scroll Speed

### Xbox Controller
- **A / B Buttons:** Play / Pause
- **DPad Left / Right:** Set Direction (Forward/Backward)
- **DPad Up / Down:** Adjust Font Size
- **X Button:** Toggle Direction
- **Y Button:** Toggle Edge Fade
- **LB / RB (Bumpers):** Adjust Scroll Speed
- **LT / RT (Triggers):** Rewind / Fast-Forward (Double-tap for Turbo)
- **Start Button:** Toggle Settings Panel
- **Back Button:** Show Info / About

## Getting Started

1. Launch the application.
2. Click **Open File** to select a `.txt` book.
3. Use the **Speed Slider** (or controller bumpers) to adjust how fast the text scrolls.
4. Click **Start** (or press Space / A / B) to begin reading.

## Dependencies

This project uses the following third-party libraries:

- [Extended.Wpf.Toolkit](https://github.com/xceedsoftware/wpftoolkit) (Xceed Community License - Free for non-commercial use)
- [SharpDX.XInput](https://github.com/sharpdx/SharpDX) (MIT License)

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
