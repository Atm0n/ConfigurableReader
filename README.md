# ConfigurableReader

A modern, highly customizable desktop reading application built with [.NET 10](https://dotnet.microsoft.com/) and [Avalonia UI](https://github.com/AvaloniaUI/Avalonia). Designed for comfortable, hands-free reading, rapid comprehension, and effortless book management from either your desk or couch.

---

## 🌟 Key Features

### 📖 Multiple Reading Modes
- **Continuous Auto-Scroll:** High-performance typography rendering with smooth variable speed, bidirectional flow, and customizable edge fading.
- **Focus Ruler / Reading Mask:** Softly dims the upper and lower text canvas, isolating the active reading line with an adjustable focus aperture and guide rails to eliminate visual distractions (ideal for ADHD, dyslexia, and deep reading focus).
- **Bionic / Focus Reading:** Enhances reading speed and fixation by dynamically bolding the initial anchor letters of each word to guide saccadic eye movements.
- **RSVP (Rapid Serial Visual Presentation):** Spritz-style word-by-word streaming with an **Optimal Recognition Point (ORP)** focal highlight, eliminating eye movement fatigue and supporting speeds up to **2,000 WPM**.

### 📚 Multi-Format & Web Article Support
- **EPUB (`.epub`):** Chapter hierarchy, metadata parsing, and embedded cover art extraction.
- **PDF (`.pdf`):** High-fidelity text and Table of Contents extraction via [PdfPig](https://github.com/UglyToad/PdfPig), including cover page graphic extraction.
- **Web & Online Articles (`http://`, `https://`, `.html`, `.htm`):** Clean Readability-style web reader powered by [HtmlAgilityPack](https://github.com/zzzprojects/html-agility-pack) that strips ads, navbars, and noise, extracting clean headings and OpenGraph preview images.
- **Microsoft Word (`.docx`):** Document text parsing via [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK).
- **Markdown (`.md`):** Clean rendering with [Markdig](https://github.com/xoofx/markdig).
- **Plain Text (`.txt`):** Fast loading of any text file.

### 🖼️ Visual Bookshelf Library
- **Cover Art Extraction:** Automatically extracts and caches book covers for EPUB, PDF, and Web articles in `%APPDATA%\ConfigurableReader\Covers\`.
- **Organized Browsing:** Filter books by title or path, with instant sorting by **Recently Read**, **Title**, or **Progress %**.
- **Instant Resume:** Automatically remembers your exact reading position and progress for every document.

### 🎨 Themes & Typography Customization
- **9 Curated Reading Themes:** Dark, Light, Sepia, Nord, Solarized Dark, Dracula, Cyberpunk, Forest, and Monochrome.
- **Full Styling Freedom:** Custom text and background color pickers, font family selector, custom font sizes (including high-accessibility sizing), and edge fade gradient controls.
- **Zen Mode (<kbd>F11</kbd> / <kbd>Z</kbd>):** Distraction-free full-screen reading mode that hides toolbars and chrome for pure immersion.

### 📊 Reading Statistics & Live Analytics
- **Live WPM Calculation:** Real-time reading speed metrics based on actual words scrolled or displayed.
- **Session Tracking:** Tracks reading duration, total words read, and calculates estimated time to finish the book.

### 🧭 Navigation & Bookmarks
- **Table of Contents:** Live chapter tree for EPUB, PDF, and structured HTML articles.
- **Custom Bookmarks:** Save, label, and revisit specific passages with one click.
- **In-Book Search:** Full-text instant search with jump-to-result navigation.

### 🎮 Dual Input System (Keyboard + Gamepad)
- **Lean-Back Couch Reading:** Full native controller support for Xbox, PlayStation, Nintendo Switch, and standard HID gamepads via [HIDDevices](https://github.com/DevDecoder/HIDDevices).
- **Focus Detection:** Gamepad inputs are only processed when the reader window is focused to prevent accidental inputs while multitasking.
- **Live Mode Indicator:** Real-time status indicator (⌨️ / 🎮) in the status bar.
- **Drag & Drop:** Drag any supported file or web URL directly into the reader window.

### 🌐 Internationalization (i18n)
- Multilingual interface supporting **English (en-US)**, **Spanish (es-ES)**, and **Catalan (ca-ES)** with seamless runtime switching.

---

## ⌨️ Controls & Shortcuts

### Keyboard Controls

| Shortcut | Action |
| :--- | :--- |
| <kbd>Space</kbd> | Play / Pause reading |
| <kbd>←</kbd> / <kbd>→</kbd> | Set direction (Backward / Forward) |
| <kbd>↑</kbd> / <kbd>↓</kbd> | Adjust font size (Tap: 1pt, Double-tap: 10pt) |
| <kbd>Ctrl</kbd> + <kbd>Scroll</kbd> | Fine font size adjustment |
| <kbd>Scroll</kbd> | Scroll through text position |
| <kbd>R</kbd> | Reverse reading direction |
| <kbd>F</kbd> | Toggle edge fade effect |
| <kbd>S</kbd> | Toggle Settings drawer |
| <kbd>T</kbd> | Cycle through color themes |
| <kbd>M</kbd> | Cycle reading mode (Continuous Scroll ➔ Focus Ruler ➔ RSVP) |
| <kbd>K</kbd> | Open Keyboard Shortcut Manager (rebind any action) |
| <kbd>U</kbd> | Open Webpage / Article dialog |
| <kbd>F11</kbd> | Toggle Zen / Fullscreen mode |
| <kbd>Esc</kbd> | Exit Zen mode |
| <kbd>+</kbd> / <kbd>-</kbd> | Increase / Decrease scroll speed (or RSVP WPM) |
| <kbd>I</kbd> | Show About / Info dialog |

### Gamepad Controls

| Button | Action |
| :--- | :--- |
| **A / B** | Play / Pause |
| **DPad Up / Down** | Adjust font size |
| **DPad Left / Right** | Set direction (Backward / Forward) |
| **X** | Reverse reading direction |
| **Y** | Toggle edge fade |
| **LB / RB (Bumpers)** | Adjust scroll speed |
| **LT / RT (Triggers)** | Rewind / Fast-forward (Hold to scroll, double-tap boost) |

> [!TIP]
> **Linux Gamepad Setup:**
> By default, Linux restricts access to raw HID device nodes (`/dev/hidraw*`) to root. To allow regular users to use gamepads with ConfigurableReader without root privileges, install the included udev rule:
> ```bash
> sudo cp packaging/linux/99-configurable-reader.rules /etc/udev/rules.d/
> sudo udevadm control --reload-rules && sudo udevadm trigger
> ```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Building and Running
```bash
# Clone the repository
git clone https://github.com/Atm0n/ConfigurableReader.git
cd ConfigurableReader

# Run the test suite
dotnet test ConfigurableReader.slnx

# Launch the application
dotnet run --project src/ConfigurableReader
```

---

## 🏗️ Architecture

```
ConfigurableReader/
├── src/
│   ├── ConfigurableReader.Core/          # Domain abstractions (IBookParser, IBookSource, BookmarkItem)
│   ├── Parsers/
│   │   ├── ConfigurableReader.Parsers.Epub/ # EPUB parsing & cover extraction (VersOne.Epub)
│   │   ├── ConfigurableReader.Parsers.Pdf/  # PDF text, TOC & cover extraction (PdfPig)
│   │   └── ConfigurableReader.Parsers.Html/ # Web article extractor & HTML parser (HtmlAgilityPack)
│   └── ConfigurableReader/              # Avalonia 12 UI Application
│       ├── Common/                      # Constants & utilities
│       ├── Models/                      # AppSettings, BookRecord, ReaderTheme
│       ├── Services/                    # ReaderService, ReaderController, CoverService, GamepadService
│       └── Views/                       # MainWindow (partial classes: Library, Input, Rendering, Bookmarks, Settings)
└── tests/
    └── ConfigurableReader.Tests/        # Unit tests using Shouldly & Microsoft.Testing.Platform
```

---

## 📦 Dependencies

- [Avalonia UI](https://github.com/AvaloniaUI/Avalonia) (MIT)
- [HIDDevices](https://github.com/DevDecoder/HIDDevices) (Apache 2.0)
- [PdfPig](https://github.com/UglyToad/PdfPig) (MIT)
- [VersOne.Epub](https://github.com/versosoftware/versone.epub) (MIT)
- [HtmlAgilityPack](https://github.com/zzzprojects/html-agility-pack) (MIT)
- [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) (MIT)
- [Markdig](https://github.com/xoofx/markdig) (BSD-2-Clause)
- [Shouldly](https://github.com/shouldly/shouldly) (Test assertion library)

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
