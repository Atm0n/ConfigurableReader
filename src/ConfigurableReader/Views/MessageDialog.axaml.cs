using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ConfigurableReader.Services;
using System;
using System.Threading.Tasks;

namespace ConfigurableReader.Views;

public partial class MessageDialog : Window
{
    private readonly GamepadService? _gamepadService;
    private readonly DateTime _creationTime = DateTime.Now;

    /// <summary>Parameterless constructor required by the Avalonia XAML compiler and designer.</summary>
    public MessageDialog() : this(null)
    {
    }

    public MessageDialog(GamepadService? gamepadService)
    {
        InitializeComponent();

        _gamepadService = gamepadService;
        if (_gamepadService != null)
        {
            _gamepadService.DialogDismissRequested += OnGamepadDismissRequested;
        }
    }

    private void OnGamepadDismissRequested()
    {
        // Ignore the press that may have opened this dialog.
        if ((DateTime.Now - _creationTime).TotalMilliseconds < 500) return;
        if (IsActive) Close();
    }

    public static async Task ShowAsync(Window owner, string message, GamepadService? gamepadService = null)
    {
        var dialog = new MessageDialog(gamepadService);
        dialog.FindControl<TextBlock>("MessageText")!.Text = message;
        await dialog.ShowDialog(owner);
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape || e.Key == Key.Enter || e.Key == Key.Space)
        {
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_gamepadService != null)
        {
            _gamepadService.DialogDismissRequested -= OnGamepadDismissRequested;
        }
        base.OnClosed(e);
    }
}
