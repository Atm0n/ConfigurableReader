using System.IO;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;

namespace ConfigurableReader;

public partial class MainWindow : Window
{
    private const string ConfigurableReaderFolder = "ConfigurableReader";
    private readonly DispatcherTimer _scrollTimer;
    private double _scrollSpeed = 0.1;
    private double _currentPosition = 0;
    private int _currentChunk = 0;
    private string _documentsPath = string.Empty;
    private string? _currentBookFileName;
    private TextChunkReader _chunkReader;
    private IEnumerator<string> _chunkEnumerator;
    private Configuration configuration;
    private BookPosition BookPosition;
    private BookPosition.Book? ActualBook;

    public MainWindow()
    {
        InitializeComponent();

        configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        if (configuration.Sections["bookPositions"] is null)
        {
            configuration.Sections.Add("bookPositions", new BookPosition());

        }

        BookPosition = (BookPosition)configuration.GetSection("bookPositions");

        _chunkReader = new TextChunkReader();
        _scrollTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };

        _scrollTimer.Tick += ScrollTimer_Tick;
        _documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ConfigurableReaderFolder);

        if (!Directory.Exists(_documentsPath))
        {
            Directory.CreateDirectory(_documentsPath);

        }

        LoadUserConfiguration();

    }

    private void LoadUserConfiguration()
    {
        FontSizeSlider.Value = Properties.Settings.Default.FontSize;
        ChangeFontSize(Properties.Settings.Default.FontSize);


        var colorName = Properties.Settings.Default.TextColor;

        ColorPicker.SelectedColor = Color.FromArgb(colorName.A, colorName.R, colorName.G, colorName.B);
        AssignNewColorToText();


        _scrollSpeed = Properties.Settings.Default.ScrollSpeed;
        SpeedSlider.Value = Properties.Settings.Default.ScrollSpeed;
    }

    private void LoadNextChunk()
    {
        if (_chunkEnumerator.MoveNext())
        {
            TextBlock.Text = _chunkEnumerator.Current.Replace("\r\n", " ").Replace("\n", " ");
        }
    }
    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new();
        if (openFileDialog.ShowDialog() == true)
        {
            _currentBookFileName = openFileDialog.FileName;

            _chunkEnumerator = _chunkReader.ReadChunks(_currentBookFileName, 1024).GetEnumerator(); //5024

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

            _currentPosition = double.Parse(File.ReadAllText(Path.Combine(_documentsPath, Path.GetFileName(_currentBookFileName))));
            ScrollViewer.ScrollToHorizontalOffset(_currentPosition);

            configuration.Save();
        }
    }
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _scrollTimer.Start();
    }
    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        _scrollTimer.Stop();

        File.WriteAllText(Path.Combine(_documentsPath, Path.GetFileName(_currentBookFileName)), $"{_currentPosition}");

    }

    private void ScrollTimer_Tick(object sender, EventArgs e)
    {
        _currentPosition = ScrollViewer.HorizontalOffset;

        _currentPosition += _scrollSpeed;
        if (ScrollViewer.HorizontalOffset == ScrollViewer.ScrollableWidth)
        {
            LoadNextChunk();
            _currentChunk++;
            _currentPosition = 0;
        }

        ScrollViewer.ScrollToHorizontalOffset(_currentPosition);
    }
    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _scrollSpeed = e.NewValue;
    }
    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ChangeFontSize(e.NewValue);
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
            ActualBook.ScrollPosition = _currentPosition;

        }
    }

    private void SaveUserConfiguration()
    {
        Properties.Settings.Default.FontSize = FontSizeSlider.Value;

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

        Properties.Settings.Default.ScrollSpeed = _scrollSpeed;

        Properties.Settings.Default.Save();
    }
}