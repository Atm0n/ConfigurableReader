using System.IO;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;
using System.Windows.Input;
using SharpDX.XInput;
using System.Text.Json;


namespace ConfigurableReader;

public partial class MainWindow : Window
{
    private readonly Controller controller = new(UserIndex.One);
    private readonly DispatcherTimer inputTimer;
    private int _currentPosition = 0;
    private bool IsPaused = true;
    private string? _currentBookFileName;
    private string _fullText = string.Empty;
    
    private List<BookRecord> _bookRecords = new();
    
    private bool isProcessingInput = false;
    private bool isReversing = false;
    private bool _isUpdatingFromCode = false;

    private double _currentOffsetX = 0;
    private DateTime _lastRenderTime;
    private readonly Dictionary<char, double> _charWidths = new();

    public MainWindow()
    {
        InitializeComponent();

        inputTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20),
        };

        XboxController();

        LoadBookPositionConfiguration();

        PopulateFontList();
        LoadUserConfiguration();

    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (IsPaused || string.IsNullOrEmpty(_fullText)) return;

        DateTime now = DateTime.Now;
        if (_lastRenderTime == DateTime.MinValue)
        {
            _lastRenderTime = now;
            return;
        }

        double deltaTime = (now - _lastRenderTime).TotalSeconds;
        _lastRenderTime = now;

        double speed = SpeedSlider.Value;
        double pixelsToMove = speed * deltaTime;

        if (isReversing)
        {
            _currentOffsetX += pixelsToMove;
            while (_currentOffsetX > 0)
            {
                if (_currentPosition > 0)
                {
                    _currentPosition--;
                    double charWidth = GetCharacterWidth(_fullText[_currentPosition]);
                    _currentOffsetX -= charWidth;
                }
                else
                {
                    _currentOffsetX = 0;
                    StartStop();
                    Xceed.Wpf.Toolkit.MessageBox.Show("Start of the book reached");
                    break;
                }
            }
        }
        else
        {
            _currentOffsetX -= pixelsToMove;
            while (true)
            {
                if (_currentPosition >= _fullText.Length)
                {
                    _currentOffsetX = 0;
                    StartStop();
                    Xceed.Wpf.Toolkit.MessageBox.Show("End of the book reached");
                    break;
                }

                double charWidth = GetCharacterWidth(_fullText[_currentPosition]);
                if (_currentOffsetX <= -charWidth)
                {
                    _currentOffsetX += charWidth;
                    _currentPosition++;
                }
                else
                {
                    break;
                }
            }
        }

        TextTranslateTransform.X = _currentOffsetX;
        
        // Pixel-Perfect Visual Centering
        // We ensure the TextBlock is centered within the Canvas based on its real rendered height.
        if (ReadingAreaCanvas.ActualHeight > 0 && TextBlock.ActualHeight > 0)
        {
            // Calculate the exact center of the screen
            double canvasCenter = ReadingAreaCanvas.ActualHeight / 2;
            
            // Calculate the exact center of the text block
            double textBlockCenter = TextBlock.ActualHeight / 2;
            
            // At massive font sizes, WPF's internal BaselineOffset can be misleading.
            // We use a simpler, robust mathematical centering:
            double top = canvasCenter - textBlockCenter;
            
            // Apply a small corrective factor for visual balance if needed, 
            // but pure mathematical center is most consistent for giant fonts.
            Canvas.SetTop(TextBlock, top);
        }
        
        UpdateDisplayedText();
    }

    private void UpdateDisplayedText()
    {
        // Increase buffer to 5000 for massive lookahead. 
        // Using a Canvas ensures these characters are pre-rendered far beyond the screen edge.
        int length = Math.Min(5000, _fullText.Length - _currentPosition);
        string newText = _fullText.Substring(_currentPosition, length);
        
        if (TextBlock.Text != newText)
        {
            TextBlock.Text = newText;
        }

        TextSlider.Value = _currentPosition;
        UpdatePercentage();
    }

    private double GetCharacterWidth(char c)
    {
        if (_charWidths.TryGetValue(c, out double width)) return width;

        FormattedText formattedText = new FormattedText(
            c.ToString(),
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(TextBlock.FontFamily, TextBlock.FontStyle, TextBlock.FontWeight, TextBlock.FontStretch),
            TextBlock.FontSize,
            TextBlock.Foreground,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        width = formattedText.WidthIncludingTrailingWhitespace;
        _charWidths[c] = width;
        return width;
    }

    private void ClearCharWidthCache()
    {
        _charWidths.Clear();
    }

    private void PopulateFontList()
    {
        FontComboBox.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
    }

    private async void DelayInputProcessing()
    {
        await Task.Delay(200);
        isProcessingInput = false;
    }

    private void LoadBookPositionConfiguration()
    {
        try
        {
            string json = Properties.Settings.Default.BookPositionsJson;
            if (!string.IsNullOrEmpty(json))
            {
                _bookRecords = JsonSerializer.Deserialize<List<BookRecord>>(json) ?? new List<BookRecord>();
            }
        }
        catch
        {
            _bookRecords = new List<BookRecord>();
        }
    }

    private void LoadUserConfiguration()
    {
        ChangeFontSize(Properties.Settings.Default.FontSize);

        var textColor = CreateColorFromDrawingColor(Properties.Settings.Default.TextColor);

        ColorPicker.SelectedColor = textColor;
        TextBlock.FontSize = Properties.Settings.Default.FontSize;
        TextBlock.Foreground = CreateBrush(textColor);

        SpeedSlider.Value = Properties.Settings.Default.ScrollSpeed;

        var backgroundColor = Properties.Settings.Default.BackgroundColor;
        this.Background = new SolidColorBrush(Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B));

        FadeCheckBox.IsChecked = Properties.Settings.Default.EnableEdgeFading;
        UpdateEdgeFading(Properties.Settings.Default.EnableEdgeFading);

        if (!string.IsNullOrEmpty(Properties.Settings.Default.FontFamily))
        {
            var fontFamily = new FontFamily(Properties.Settings.Default.FontFamily);
            TextBlock.FontFamily = fontFamily;
            FontComboBox.SelectedItem = Fonts.SystemFontFamilies.FirstOrDefault(f => f.Source == fontFamily.Source);
        }
    }

    private static Color CreateColorFromDrawingColor(System.Drawing.Color textColor)
    {
        return Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B);
    }

    private void UpdatePercentage()
    {
        if (_fullText.Length > 0)
        {
            double percentage = (double)_currentPosition / _fullText.Length * 100;
            PercentageText.Text = $"{percentage:F1}%";
        }
    }

    private void ChangeFontSize(double value)
    {
        if (TextBlock is not null)
            TextBlock.FontSize = value;
    }

    private static SolidColorBrush CreateBrush(Color? color)
    {
        if (color is not null)
            return new SolidColorBrush((Color)color);
        return new SolidColorBrush();
    }

    #region configuration
    private void SaveActualBookConfiguration()
    {
        if (_currentBookFileName is not null)
        {
            string bookName = Path.GetFileName(_currentBookFileName);
            var record = _bookRecords.FirstOrDefault(r => r.Name == bookName);
            
            if (record == null)
            {
                record = new BookRecord { Name = bookName };
                _bookRecords.Add(record);
            }
            
            record.ScrollPosition = _currentPosition;
            
            string json = JsonSerializer.Serialize(_bookRecords);
            Properties.Settings.Default.BookPositionsJson = json;
        }
    }

    private void SaveUserConfiguration()
    {
        Properties.Settings.Default.FontSize = (int)TextBlock.FontSize;

        if (ColorPicker.SelectedColor is not null)
        {
            var color = ColorPicker.SelectedColor.Value;
            Properties.Settings.Default.TextColor = System.Drawing.Color.FromArgb
            (
                color.A,
                color.R,
                color.G,
                color.B
            );
        }
        if (BackgroundColorPicker.SelectedColor is not null)
        {
            var backgroundColor = BackgroundColorPicker.SelectedColor.Value;
            Properties.Settings.Default.BackgroundColor = System.Drawing.Color.FromArgb
            (
                backgroundColor.A,
                backgroundColor.R,
                backgroundColor.G,
                backgroundColor.B
            );


        }

        Properties.Settings.Default.ScrollSpeed = SpeedSlider.Value;

        if (TextBlock.FontFamily != null)
        {
            Properties.Settings.Default.FontFamily = TextBlock.FontFamily.Source;
        }

        Properties.Settings.Default.EnableEdgeFading = FadeCheckBox.IsChecked ?? true;

        Properties.Settings.Default.Save();
    }

    #endregion
    #region Events
    private void UpdateEdgeFading(bool enable)
    {
        if (ReadingAreaCanvas != null)
        {
            ReadingAreaCanvas.OpacityMask = enable ? (Brush)FindResource("EdgeFadeMask") : null;
        }
    }

    private void FadeCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        UpdateEdgeFading(FadeCheckBox.IsChecked ?? true);
    }

    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontComboBox.SelectedItem is FontFamily fontFamily)
        {
            TextBlock.FontFamily = fontFamily;
            ClearCharWidthCache();
        }
    }

    #region xboxController
    private void InputXboxTimer_Tick(object? sender, EventArgs e)
    {
        if (controller.IsConnected && !isProcessingInput)
        {
            var state = controller.GetState();
            var gamepad = state.Gamepad;
            if ((gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0)
            {
                isProcessingInput = true;
                isReversing = false;
                DelayInputProcessing();

            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0)
            {
                isProcessingInput = true;
                isReversing = true;
                DelayInputProcessing();

            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0)
            {
                isProcessingInput = true;
                TextBlock.FontSize += 1;
                ClearCharWidthCache();
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0)
            {
                isProcessingInput = true;
                TextBlock.FontSize -= 1;
                ClearCharWidthCache();
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & (GamepadButtonFlags.A | GamepadButtonFlags.B)) != 0)
            {
                isProcessingInput = true;
                StartStop();
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.X) != 0)
            {
                isProcessingInput = true;
                isReversing = !isReversing;
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.Y) != 0)
            {
                isProcessingInput = true;
                FadeCheckBox.IsChecked = !FadeCheckBox.IsChecked;
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0)
            {
                isProcessingInput = true;
                SpeedSlider.Value -= 10;
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0)
            {
                isProcessingInput = true;
                SpeedSlider.Value += 10;
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.Start) != 0)
            {
                isProcessingInput = true;
                SettingsExpander.IsExpanded = !SettingsExpander.IsExpanded;
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.Back) != 0)
            {
                isProcessingInput = true;
                InfoButton_Click(this, new RoutedEventArgs());
                DelayInputProcessing();
            }
        }
    }

    private void XboxController()
    {
        inputTimer.Tick += InputXboxTimer_Tick;
        inputTimer.Start();
    }
    #endregion xboxController
    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new()
        {
            Filter = "Text files (*.txt)|*.txt"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            // Save previous book progress before opening new one
            SaveActualBookConfiguration();

            _currentBookFileName = openFileDialog.FileName;
            string bookName = Path.GetFileName(_currentBookFileName);

            _fullText = File.ReadAllText(_currentBookFileName).Replace("\r", " ").Replace("\n", " ").Replace("  ", " "); ;

            var actualBook = _bookRecords.FirstOrDefault(r => r.Name == bookName);

            if (actualBook is null)
            {
                actualBook = new BookRecord { Name = bookName };
                _bookRecords.Add(actualBook);
            }

            _isUpdatingFromCode = true;
            try
            {
                _currentPosition = actualBook.ScrollPosition;
                _currentOffsetX = 0;
                TextTranslateTransform.X = 0;

                TextSlider.Maximum = _fullText.Length;
                TextSlider.Value = _currentPosition;
                BookNameText.Text = bookName;
                
                UpdateDisplayedText();
                UpdatePercentage();
            }
            finally
            {
                _isUpdatingFromCode = false;
            }

            SaveUserConfiguration(); // This saves both user settings and book positions
        }
    }

    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Speed is now handled in OnRendering
    }

    private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {
        TextBlock.Foreground = CreateBrush(e.NewValue);
        ClearCharWidthCache();
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not null)
        {
            ChangeFontSize((int)e.NewValue);
            ClearCharWidthCache();
        }
    }

    private void BackgroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {
        if (e.NewValue.HasValue)
        {
            this.Background = new SolidColorBrush(e.NewValue.Value);
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        this.Focus();

        switch (e.Key)
        {
            case Key.Left:
                isReversing = true;
                break;
            case Key.Right:
                isReversing = false;
                break;
            case Key.Space:
                StartStop();
                break;
            case Key.Up:
                TextBlock.FontSize += 1;
                ClearCharWidthCache();
                break;
            case Key.Down:
                TextBlock.FontSize -= 1;
                ClearCharWidthCache();
                break;
            case Key.R:
                isReversing = !isReversing;
                break;
            case Key.F:
                FadeCheckBox.IsChecked = !FadeCheckBox.IsChecked;
                break;
            case Key.S:
                SettingsExpander.IsExpanded = !SettingsExpander.IsExpanded;
                break;
            case Key.I:
                InfoButton_Click(this, new RoutedEventArgs());
                break;
            case Key.OemPlus:
            case Key.Add:
                SpeedSlider.Value += 10;
                break;
            case Key.OemMinus:
            case Key.Subtract:
                SpeedSlider.Value -= 10;
                break;
            default:
                break;
        }
    }
    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        StartStop();
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveActualBookConfiguration();
        SaveUserConfiguration();
    }

    #endregion
    private void StartStop()
    {
        if (_currentBookFileName is not null)
        {
            if (IsPaused)
            {
                StartStopButton.Content = "Stop";
                IsPaused = false;
                _lastRenderTime = DateTime.MinValue;
                CompositionTarget.Rendering += OnRendering;
                SettingsExpander.IsExpanded = false;
            }
            else
            {
                StartStopButton.Content = "Start";
                IsPaused = true;
                CompositionTarget.Rendering -= OnRendering;
            }
        }

    }

    private void TextSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IsPaused && !_isUpdatingFromCode)
        {
            _currentPosition = (int)TextSlider.Value;
            _currentOffsetX = 0;
            TextTranslateTransform.X = 0;
            UpdateDisplayedText();
        }
    }

    private void ReverseButton_Click(object sender, RoutedEventArgs e)
    {
        isReversing = !isReversing;
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        InfoDialog infoDialog = new InfoDialog
        {
            Owner = this
        };
        infoDialog.ShowDialog();
    }
}
