using System.IO;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;
using System.Windows.Input;
using SharpDX.XInput;


namespace ConfigurableReader;

public partial class MainWindow : Window
{
    private Controller controller = new(UserIndex.One);
    private DispatcherTimer inputTimer;
    private readonly DispatcherTimer _textUpdateTimer;
    private int _currentPosition = 0;
    private bool IsPaused = true;
    private string? _currentBookFileName;
    private string _fullText;
    private Configuration configuration;
    private BookPosition BookPosition;
    private BookPosition.Book? ActualBook;
    private bool isProcessingInput = false;
    private bool isReversing = false;

    public MainWindow()
    {
        InitializeComponent();

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
    }

    private void LoadUserConfiguration()
    {
        ChangeFontSize(Properties.Settings.Default.FontSize);

        var textColor = CreateColorFromDrawingColor(Properties.Settings.Default.TextColor);

        ColorPicker.SelectedColor = textColor;
        TextBlock.FontSize = Properties.Settings.Default.FontSize;
        TextBlock.Foreground = CreateBrush(textColor);

        SpeedSlider.Value = Properties.Settings.Default.ScrollSpeed;

        var backgroundColor = Properties.Settings.Default.BackgoundColor;
        this.Background = new SolidColorBrush(Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B));
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
                Xceed.Wpf.Toolkit.MessageBox.Show("Start of the book reached");
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
                Xceed.Wpf.Toolkit.MessageBox.Show("End of the book reached");
            }
        }

        TextBlock.Text = _fullText.Substring(_currentPosition, _fullText.Length - _currentPosition);
        TextSlider.Value = _currentPosition;
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
        if (ActualBook is not null)
        {
            ActualBook.ScrollPosition = _currentPosition;
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
            var backgoundColor = BackgroundColorPicker.SelectedColor.Value;
            Properties.Settings.Default.BackgoundColor = System.Drawing.Color.FromArgb
            (
                backgoundColor.A,
                backgoundColor.R,
                backgoundColor.G,
                backgoundColor.B
            );


        }

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
        //XBOX controller
        inputTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
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
            _currentBookFileName = openFileDialog.FileName;

            _fullText = File.ReadAllText(_currentBookFileName).Replace("\r", " ").Replace("\n", " ").Replace("  ", " "); ;

            ActualBook = BookPosition.Books.FirstOrDefault(book => book.Name == Path.GetFileName(_currentBookFileName));

            if (ActualBook is null)
            {

                ActualBook = new BookPosition.Book()
                {
                    Name = Path.GetFileName(_currentBookFileName),
                };
                BookPosition.Books.Add(ActualBook);
            }

            _currentPosition = ActualBook.ScrollPosition;

            TextBlock.Text = _fullText.Substring(_currentPosition, _fullText.Length - _currentPosition);

            TextSlider.Maximum = _fullText.Length;

            configuration.Save();
        }
    }

    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _textUpdateTimer.Interval = TimeSpan.FromSeconds(e.NewValue);
    }

    private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {
        TextBlock.Foreground = CreateBrush(e.NewValue);
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not null)
        {
            ChangeFontSize((int)e.NewValue);
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
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveActualBookConfiguration();
        SaveUserConfiguration();

        configuration.Save();

    }

    #endregion
    private void StartStop()
    {
        if (_currentBookFileName is not null)
        {
            if (IsPaused)
            {
                StartStopButton.Content = "Start";
                IsPaused = !IsPaused;
                _textUpdateTimer.Start();
            }
            else
            {
                StartStopButton.Content = "Stop";
                IsPaused = !IsPaused;
                _textUpdateTimer.Stop();

            }
        }

    }

    private void TextSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IsPaused)
        {
            _currentPosition = (int)TextSlider.Value;
            TextBlock.Text = _fullText.Substring(_currentPosition, _fullText.Length - _currentPosition);
        }
    }

    private void ReverseButton_Click(object sender, RoutedEventArgs e)
    {
        isReversing = !isReversing;
    }
}