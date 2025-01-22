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
    private readonly DispatcherTimer _scrollTimer;
    private double _scrollSpeed = 0.1;
    private double _currentPosition = 0;
    private int _currentChunk = 0;
    private bool IsPaused = true;
    private string? _currentBookFileName;
    private TextChunkReader _chunkReader;
    private IEnumerator<string> _chunkEnumerator;
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

        configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        LoadBookPositionConfiguration();

        _chunkReader = new TextChunkReader();
        _scrollTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };

        _scrollTimer.Tick += ScrollTimer_Tick;

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

    private bool LoadNextChunk()
    {
        bool hasNextChunk = _chunkEnumerator.MoveNext();
        if (hasNextChunk)
        {
            TextBlock.Text = _chunkEnumerator.Current.Replace("\r", " ").Replace("\n", " ");
            Title = $"Chunk {_currentChunk}";
        }
        return hasNextChunk;
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

            _chunkEnumerator = _chunkReader.ReadChunks(_currentBookFileName, 524).GetEnumerator(); //5024

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

            for (int i = 0; i <= ActualBook.Chunk; i++)
            {
                LoadNextChunk();

            }
            _currentChunk = ActualBook.Chunk;
            _currentPosition = ActualBook.ScrollPosition;

            ScrollViewer.ScrollToHorizontalOffset(_currentPosition);


            configuration.Save();
        }
    }

    private void StartSmoothScroll()
    {
        _scrollTimer.Interval = TimeSpan.FromMilliseconds(0.0001);
        _scrollTimer.Tick += ScrollTimer_Tick;
        _scrollTimer.Start();
    }
    private void ScrollTimer_Tick(object sender, EventArgs e)
    {
        _currentPosition = ScrollViewer.HorizontalOffset;
        _currentPosition += SpeedSlider.Value * _scrollSpeed;

        if (_currentPosition >= ScrollViewer.ScrollableWidth)
        {
            if (LoadNextChunk())
            {
                _currentChunk++;
                _currentPosition = 0;
            }
            else
            {
                StartStop();
            }
        }

        ScrollViewer.ScrollToHorizontalOffset(_currentPosition);
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
            ActualBook.Chunk = _currentChunk;
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
        if(_chunkEnumerator is not null)
        {
            if (IsPaused)
            {
                StartStopButton.Content = "Start";
                IsPaused = !IsPaused;
                StartSmoothScroll();
            }
            else
            {
                StartStopButton.Content = "Stop";
                IsPaused = !IsPaused;
                _scrollTimer.Stop();
            }
        }
        
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        StartStop();
    }
}