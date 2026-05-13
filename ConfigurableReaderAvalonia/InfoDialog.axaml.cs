using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ConfigurableReaderAvalonia;

public partial class InfoDialog : Window
{
    public InfoDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
