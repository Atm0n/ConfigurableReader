using Avalonia.Controls;
using Avalonia.Interactivity;

using System;
using Avalonia.Input;
using Avalonia.Threading;
using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;

namespace ConfigurableReaderAvalonia;

public partial class InfoDialog : Window
{
    private readonly Devices _devices = new();
    private IDisposable? _gamepadSubscription;
    private readonly DateTime _creationTime = DateTime.Now;

    public InfoDialog()
    {
        InitializeComponent();

        _gamepadSubscription = _devices.Controllers<Gamepad>().Subscribe(gamepad =>
        {
            gamepad.Connect();
            gamepad.Changes.Subscribe(_ =>
            {
                if ((DateTime.Now - _creationTime).TotalMilliseconds < 500) return;

                if (gamepad.BButton || gamepad.AButton || gamepad.Start || gamepad.Select)
                {
                    Dispatcher.UIThread.Post(Close);
                }
            });
        });
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
        _gamepadSubscription?.Dispose();
        _devices.Dispose();
        base.OnClosed(e);
    }
}
