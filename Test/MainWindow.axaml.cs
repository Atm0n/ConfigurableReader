using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Threading.Tasks;
using System;
using SharpDX.XInput;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Avalonia.Platform.Storage;
using Avalonia.Input;

namespace Test;

public partial class MainWindow : Window
{
    private readonly Controller controller = new(UserIndex.One);
    private readonly DispatcherTimer inputTimer;
    private readonly DispatcherTimer _textUpdateTimer;
    private int _currentPosition = 0;
    private bool IsPaused = true;
    private string? _currentBookFileName;
    private string _fullText = string.Empty;
    private readonly Configuration configuration;
    private BookPosition? BookPosition;
    private BookPosition.Book? ActualBook;
    private bool isProcessingInput = false;
    private bool isReversing = false;

    public MainWindow()
    {
        InitializeComponent();

        inputTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };

        XboxController();

        _textUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1), // Adjust interval as needed
        };
        _textUpdateTimer.Tick += UpdateText;

        configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        LoadBookPositionConfiguration();

        LoadUserConfiguration();
    }

    private async void DelayInputProcessing()
    {
        await Task.Delay(200);
        isProcessingInput = false;
    }

    private void LoadBookPositionConfiguration()
    {
        if (configuration.Sections["bookPositions"] is null)
        {
            configuration.Sections.Add("bookPositions", new BookPosition());
        }

        BookPosition = (BookPosition)configuration.GetSection("bookPositions");
        if (BookPosition is null) throw new Exception("BookPosition is null");
    }

    private void LoadUserConfiguration()
    {
        ChangeFontSize(Properties.Settings.Default.FontSize);

        var textColor = CreateColorFromDrawingColor(Properties.Settings.Default.TextColor);

        ColorPicker.Color = textColor;
        TextBlock.FontSize = Properties.Settings.Default.FontSize;
        FontSizeSlider.Value = Properties.Settings.Default.FontSize;
        TextBlock.Foreground = CreateBrush(textColor);

        SpeedSlider.Value = Properties.Settings.Default.ScrollSpeed;

        var backgroundColor = Properties.Settings.Default.BackgoundColor;
        this.Background = new SolidColorBrush(Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B));
        BackgroundColorPicker.Color = CreateColorFromDrawingColor(backgroundColor);
    }

    private static Color CreateColorFromDrawingColor(System.Drawing.Color textColor)
    {
        return Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B);
    }

    private void UpdateText(object sender, EventArgs e)
    {
        int modifier = isReversing ? -1 : 1;
        UpdateTextBlock(modifier);
    }

    private void UpdateTextBlock(int modifier)
    {
        if (_currentPosition > 0 && isReversing)
        {
            _currentPosition += modifier;
            if (_currentPosition < 0)
            {
                _currentPosition = 0;
                StartStop();
                // MessageBox.Show("Start of the book reached");
                isReversing = false;
            }
        }
        else if (_currentPosition < _fullText.Length && !isReversing)
        {
            _currentPosition += modifier;
            if (_currentPosition > _fullText.Length)
            {
                _currentPosition = _fullText.Length;
                StartStop();
                // MessageBox.Show("End of the book reached");
            }
        }

        TextBlock.Text = _fullText.Substring(_currentPosition, Math.Min(200, _fullText.Length - _currentPosition));
        TextSlider.Value = _currentPosition;
    }

    private void ChangeFontSize(double value)
    {
        if (TextBlock is not null)
            TextBlock.FontSize = value;
    }

    private static SolidColorBrush CreateBrush(Color? color)
    {
        return color is not null ? new SolidColorBrush((Color)color) : new SolidColorBrush();
    }

    #region configuration
    private void SaveActualBookConfiguration()
    {
        if (ActualBook is not null)
        {
            ActualBook.ScrollPosition = _currentPosition;
        }
    }

    private void SaveUserConfiguration()
    {
        Properties.Settings.Default.FontSize = (int)TextBlock.FontSize;

        var color = ColorPicker.Color;
        Properties.Settings.Default.TextColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        var backgoundColor = BackgroundColorPicker.Color;
        Properties.Settings.Default.BackgoundColor = System.Drawing.Color.FromArgb(backgoundColor.A, backgoundColor.R, backgoundColor.G, backgoundColor.B);

        Properties.Settings.Default.ScrollSpeed = SpeedSlider.Value;

        Properties.Settings.Default.Save();
    }
    #endregion

    #region Events
    #region xboxController
    private void InputXboxTimer_Tick(object sender, EventArgs e)
    {
        if (controller.IsConnected && !isProcessingInput)
        {
            var state = controller.GetState();
            var gamepad = state.Gamepad;
            if ((gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0)
            {
                isProcessingInput = true;
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0)
            {
                isProcessingInput = true;
                DelayInputProcessing();
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.A) != 0)
            {
                isProcessingInput = true;
                StartStop();
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

    private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open file",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { FilePickerFileTypes.TextPlain }
        });

        if (files.Count >= 1)
        {
            _currentBookFileName = files[0].Name;

            using (Stream fileStream = await files[0].OpenReadAsync())
            using (StreamReader reader = new(fileStream))
            {
                _fullText = (await reader.ReadToEndAsync()).Replace("\r", " ").Replace("\n", " ").Replace("  ", " ");
            }

            ActualBook = BookPosition!.Books.FirstOrDefault(book => book.Name == Path.GetFileName(_currentBookFileName));

            if (ActualBook is null)
            {
                ActualBook = new BookPosition.Book()
                {
                    Name = Path.GetFileName(_currentBookFileName),
                };
                BookPosition!.Books.Add(ActualBook);
            }

            _currentPosition = ActualBook.ScrollPosition;

            TextBlock.Text = _fullText.Substring(_currentPosition, Math.Min(200, _fullText.Length - _currentPosition));

            //TextBlock.Text = _fullText.Substring(_currentPosition, _fullText.Length - _currentPosition);

            TextSlider.Maximum = _fullText.Length;

            configuration.Save();
        }
    }

    private void SpeedSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        _textUpdateTimer.Interval = TimeSpan.FromSeconds(e.NewValue);
    }

    private void ColorPicker_ColorChanged(object? sender, Avalonia.Controls.ColorChangedEventArgs e)
    {
        TextBlock.Foreground = CreateBrush(e.NewColor);
    }

    private void BackgroundColorPicker_ColorChanged(object? sender, Avalonia.Controls.ColorChangedEventArgs e)
    {
        this.Background = new SolidColorBrush(e.NewColor);
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
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
                break;
            case Key.Down:
                TextBlock.FontSize -= 1;
                break;
            default:
                break;
        }
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        StartStop();
    }

    
    #endregion

    private void StartStop()
    {
        if (_currentBookFileName is not null)
        {
            IsPaused = !IsPaused;
            StartStopButton.Content = IsPaused ? "Start" : "Stop";
            if (IsPaused)
            {
                _textUpdateTimer.Stop();
            }
            else
            {
                _textUpdateTimer.Start();
            }
        }
    }

    private void ReverseButton_Click(object sender, RoutedEventArgs e)
    {
        isReversing = !isReversing;
    }

    private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (IsPaused)
        {
            _currentPosition = (int)TextSlider.Value;
            TextBlock.Text = _fullText.Substring(_currentPosition, Math.Min(200, _fullText.Length - _currentPosition));

            //TextBlock.Text = _fullText.Substring(_currentPosition, _fullText.Length - _currentPosition);
        }
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        SaveActualBookConfiguration();
        SaveUserConfiguration();
        configuration.Save();
    }

    private void NumericUpDown_ValueChanged(object? sender, Avalonia.Controls.NumericUpDownValueChangedEventArgs e)
    {
        if (e.NewValue is not null)
        {
            ChangeFontSize((int)e.NewValue);
        }
    }
}
