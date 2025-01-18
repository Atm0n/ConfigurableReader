using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ConfigurableReader;

public partial class MainWindow : Window
{
    private const string ConfigurableReaderFolder = "ConfigurableReader";
    private DispatcherTimer _scrollTimer;
    private double _scrollSpeed = 0.1; // Adjust this value for different speeds
    private double _currentPosition = 0;
    private string _documentsPath = string.Empty;
    private string _currentBookFileName = string.Empty;
    public MainWindow()
    {
        InitializeComponent();
        _scrollTimer = new DispatcherTimer();
        _scrollTimer.Interval = TimeSpan.FromMilliseconds(100); // Adjust for different intervals
        _scrollTimer.Tick += ScrollTimer_Tick;
        _documentsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ConfigurableReaderFolder);

        if (!Directory.Exists(_documentsPath))
        { // Create the directory
            Directory.CreateDirectory(_documentsPath);
        }
    }
    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            _currentBookFileName = openFileDialog.FileName;
            string text = File.ReadAllText(_currentBookFileName);
            TextBlock.Text = text.Replace("\r\n", " ").Replace("\n", " ");


            if (File.Exists(System.IO.Path.Combine(_documentsPath, System.IO.Path.GetFileName(_currentBookFileName))))
            {
                _currentPosition = double.Parse(File.ReadAllText(System.IO.Path.Combine(_documentsPath, System.IO.Path.GetFileName(_currentBookFileName))));
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

        File.WriteAllText(System.IO.Path.Combine(_documentsPath, System.IO.Path.GetFileName(_currentBookFileName)), $"{_currentPosition}");

    }

    private void ScrollTimer_Tick(object sender, EventArgs e)
    {
        //_currentPosition= ScrollViewer.HorizontalOffset;

        _currentPosition += _scrollSpeed;
        if (_currentPosition >= TextBlock.ActualWidth)
        {
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


    private void TextBlock_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
    {

    }
}