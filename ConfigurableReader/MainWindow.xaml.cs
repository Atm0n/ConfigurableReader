using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ConfigurableReader;

public partial class MainWindow : Window
{
    private const string ConfigurableReaderFolder = "ConfigurableReader";
    private DispatcherTimer _scrollTimer;
    private double _scrollSpeed = 0.1;
    private double _currentPosition = 0;
    private string _documentsPath = string.Empty;
    private string _currentBookFileName = string.Empty;
    private TextChunkReader _chunkReader;
    private IEnumerator<string> _chunkEnumerator;

    public MainWindow()
    {
        InitializeComponent();
        _chunkReader = new TextChunkReader();
        _scrollTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };

        _scrollTimer.Tick += ScrollTimer_Tick;
        _documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ConfigurableReaderFolder);

        if (!Directory.Exists(_documentsPath))
        { 
            // Create the directory
            Directory.CreateDirectory(_documentsPath);
        }
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

            
            _chunkEnumerator = _chunkReader.ReadChunks(_currentBookFileName, 5024).GetEnumerator();

            LoadNextChunk();

            if (File.Exists(Path.Combine(_documentsPath, Path.GetFileName(_currentBookFileName))))
            {
                _currentPosition = double.Parse(File.ReadAllText(Path.Combine(_documentsPath, Path.GetFileName(_currentBookFileName))));
                ScrollViewer.ScrollToHorizontalOffset(_currentPosition);
            }

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
            _currentPosition = 0;
        }

        ScrollViewer.ScrollToHorizontalOffset(_currentPosition);
    }
    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _scrollSpeed = e.NewValue;
    }
    private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ColorComboBox.SelectedItem != null)
        {
            ComboBoxItem selectedItem = (ComboBoxItem)ColorComboBox.SelectedItem;
            string colorName = selectedItem.Content.ToString();
            TextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));
        }
    }
    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextBlock is not null)
            TextBlock.FontSize = e.NewValue;
    }

}