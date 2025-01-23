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
    private Controller controller;
    private DispatcherTimer inputTimer;
    private readonly DispatcherTimer _textUpdateTimer;
    private readonly DispatcherTimer _scrollTimer;
    private double _scrollSpeed = 0.1;
    private int? _currentPosition;
    private bool IsPaused = true;
    private string? _currentBookFileName;
    private string _fullText;
    private Configuration configuration;
    private BookPosition BookPosition;
    private BookPosition.Book? ActualBook;
    private bool isProcessingInput = false;

    public MainWindow()
    {
        InitializeComponent();

        //XBOX controller
        controller = new Controller(UserIndex.One);
        inputTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(10),
        };
        inputTimer.Tick += InputTimer_Tick;
        inputTimer.Start();

        _textUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(10) // Adjust interval as needed
        };
        _textUpdateTimer.Tick += UpdateText;

        configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        LoadBookPositionConfiguration();

        LoadUserConfiguration();

    }

    private void InputTimer_Tick(object sender, EventArgs e)
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
        FontSizeSlider.Value = Properties.Settings.Default.FontSize;
        ChangeFontSize(Properties.Settings.Default.FontSize);

        var textColor = Properties.Settings.Default.TextColor;

        ColorPicker.SelectedColor = Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B);
        AssignNewColorToText();

        _scrollSpeed = Properties.Settings.Default.ScrollSpeed;
        SpeedSlider.Value = Properties.Settings.Default.ScrollSpeed;

        var backgroundColor = Properties.Settings.Default.BackgoundColor;
        this.Background = new SolidColorBrush(Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B));
    }


    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new()
        {
            Filter = "Text files (*.txt)|*.txt"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            _currentBookFileName = openFileDialog.FileName;

            //_chunkEnumerator = _chunkReader.ReadChunks(_currentBookFileName, 524).GetEnumerator(); //5024

            _fullText = File.ReadAllText(_currentBookFileName).Replace("\r", " ").Replace("\n", " "); ;

            ActualBook = BookPosition.Books.FirstOrDefault(book => book.Name == Path.GetFileName(_currentBookFileName));

            if (ActualBook is null)
            {

                ActualBook = new BookPosition.Book()
                {
                    Name = Path.GetFileName(_currentBookFileName),
                    Chunk = 0,
                    ScrollPosition = 0
                };
                BookPosition.Books.Add(ActualBook);
            }

            _currentPosition = (int)ActualBook.ScrollPosition;

            TextBlock.Text = _fullText.Substring((int)_currentPosition, _fullText.Length - (int)_currentPosition);

            ScrollViewer.ScrollToHorizontalOffset((int)_currentPosition);


            configuration.Save();
        }
    }
    private void UpdateText(object sender, EventArgs e)
    {
        if (_currentPosition < _fullText.Length)
        {
            TextBlock.Text = _fullText.Substring((int)_currentPosition, _fullText.Length - (int)_currentPosition);
            _currentPosition += 1; // Move forward by one character
        }
        else
        {
            StartStop();
            Microsoft.Win32.OpenFolderDialog a =new();
            a.ShowDialog();
        }
    }


    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _scrollSpeed = e.NewValue;
    }

    private void ChangeFontSize(double value)
    {
        if (TextBlock is not null)
            TextBlock.FontSize = value;
    }

    private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {
        AssignNewColorToText();
    }

    private void AssignNewColorToText()
    {
        if (ColorPicker.SelectedColor is not null)
        {
            TextBlock.Foreground = new SolidColorBrush((Color)ColorPicker.SelectedColor);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveActualBookConfiguration();
        SaveUserConfiguration();

        configuration.Save();

    }

    private void SaveActualBookConfiguration()
    {
        if (ActualBook is not null)
        {
            ActualBook.ScrollPosition = ScrollViewer.HorizontalOffset;

        }
    }

    private void SaveUserConfiguration()
    {
        if (FontSizeSlider.Value is not null)
        {
            Properties.Settings.Default.FontSize = FontSizeSlider.Value.Value;
        }

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

        Properties.Settings.Default.ScrollSpeed = _scrollSpeed;

        Properties.Settings.Default.Save();
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
        if (e.Key == Key.Space)
        {
            StartStop();
        }
    }

    private void StartStop()
    {
        if (_currentPosition is not null)
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
                _scrollTimer.Stop();
                _textUpdateTimer.Stop();

            }
        }

    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        StartStop();
    }
}