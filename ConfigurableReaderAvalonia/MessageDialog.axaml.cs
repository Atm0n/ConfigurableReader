using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ConfigurableReaderAvalonia;

public partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    public static void Show(Window owner, string message)
    {
        var dialog = new MessageDialog();
        dialog.FindControl<TextBlock>("MessageText")!.Text = message;
        dialog.ShowDialog(owner);
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
