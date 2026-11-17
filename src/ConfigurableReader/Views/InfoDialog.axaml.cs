using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ConfigurableReader.Services;
using System;

namespace ConfigurableReader.Views;

public partial class InfoDialog : Window
{
    private readonly GamepadService? _gamepadService;
    private readonly DateTime _creationTime = DateTime.Now;

    /// <summary>Parameterless constructor required by the Avalonia XAML compiler and designer.</summary>
    public InfoDialog() : this(null)
    {
    }

    public InfoDialog(GamepadService? gamepadService)
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

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
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
