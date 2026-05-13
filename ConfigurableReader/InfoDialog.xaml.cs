using System.Windows;

namespace ConfigurableReader;

public partial class InfoDialog : Window
{
    public InfoDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
