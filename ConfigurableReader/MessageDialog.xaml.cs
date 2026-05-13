using System.Windows;
using System.Windows.Threading;
using SharpDX.XInput;

namespace ConfigurableReader;

public partial class MessageDialog : Window
{
    private readonly Controller controller = new(UserIndex.One);
    private readonly DispatcherTimer inputTimer;
    private readonly DateTime _creationTime = DateTime.Now;

    public MessageDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;

        inputTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };
        inputTimer.Tick += InputXboxTimer_Tick;
        inputTimer.Start();
    }

    private void InputXboxTimer_Tick(object? sender, EventArgs e)
    {
        if ((DateTime.Now - _creationTime).TotalMilliseconds < 500) return;

        if (controller.IsConnected)
        {
            var state = controller.GetState();
            if ((state.Gamepad.Buttons & (GamepadButtonFlags.B | GamepadButtonFlags.Back | GamepadButtonFlags.A | GamepadButtonFlags.Start)) != 0)
            {
                CloseDialog();
            }
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        CloseDialog();
    }

    private void CloseDialog()
    {
        inputTimer.Stop();
        this.Close();
    }

    public static void Show(Window owner, string message)
    {
        var dialog = new MessageDialog(message)
        {
            Owner = owner
        };
        dialog.ShowDialog();
    }
}