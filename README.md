# ConfigurableReader

A highly customizable, cross-platform text reader designed for a comfortable and hands-free reading experience. Built with [Avalonia UI](https://github.com/AvaloniaUI/Avalonia).

## Key Features

- **Layout-Driven Precision:** Uses a high-performance rendering engine that accounts for font kerning and typographic details for perfectly smooth scrolling.
- **Multi-Format Support:** Read books in **TXT, EPUB, PDF, DOCX, and Markdown**.
- **Auto-Scrolling:** Adjust scroll speed to match your reading pace exactly.
- **Visual Customization:**
  - Change font size (supports massive fonts for accessibility).
  - Customize text and background colors.
  - Edge fading for better focus.
- **Progress Tracking:** Automatically saves your last position in each book you read.
- **Navigation:**
  - Full-text search capability.
  - Automatic Table of Contents extraction for EPUB and PDF files.
  - Custom Bookmarks feature allows saving specific locations with personalized names.
- **Gamepad Support:** Control your reading experience from the comfort of your couch using any standard HID Gamepad (Xbox, PlayStation, Switch, etc.).
- **Visual Indicators:** Connection indicator shows whether you are in Keyboard (⌨️) or Gamepad (🎮) mode.

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
2. Click **Open File** to select a book (Supports `.txt`, `.epub`, `.pdf`, `.docx`, `.md`).
3. Use the **Speed Slider** (or controller bumpers) to adjust how fast the text scrolls.
4. Click **Start** (or press Space / A / B) to begin reading.

## Dependencies

This project uses the following third-party libraries:

- [Avalonia](https://github.com/AvaloniaUI/Avalonia) (MIT License)
- [HIDDevices](https://github.com/DevDecoder/HIDDevices) (Apache License 2.0)
- [PdfPig](https://github.com/UglyToad/PdfPig) (MIT License) - PDF Parsing
- [Markdig](https://github.com/xoofx/markdig) (BSD-2-Clause) - Markdown Parsing
- [OpenXML SDK](https://github.com/dotnet/Open-XML-SDK) (MIT License) - DOCX Parsing
- [HtmlAgilityPack](https://github.com/zzzprojects/html-agility-pack) (MIT License) - HTML Cleaning
- [VersOne.Epub](https://github.com/versosoftware/versone.epub) (MIT License) - EPUB Parsing

## Future Roadmap

Looking to contribute or wondering what's next? Here are some planned improvements:

- **[X] Library View:** A central hub to manage your books, see recent reads, and view reading progress at a glance.
- **[ ] Unit Testing:** Implementation of a robust test suite for parsers, rendering logic, and localization.
- **[x] Search & Navigation:**
  - [x] Full-text search within the current book.
  - [x] Table of Contents support for EPUB and PDF.
  - [x] Custom Bookmarks.
- **[x] Advanced Performance:** Implement text chunking/virtualization for instantaneous loading of extremely large books.
- **[x] UI/UX Polish:**
  - Theme presets (Sepia, High Contrast, etc.).
  - Smooth animated transitions.
  - Modernized About and Settings dialogs.
- **[x] CI/CD:** Automated builds and releases via GitHub Actions.
- [ ] Add speed reading mode with part of the word in bold.

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
