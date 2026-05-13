using System.Windows;
using System.Windows.Threading;
using SharpDX.XInput;

namespace ConfigurableReader;

public partial class InfoDialog : Window
{
    private readonly Controller controller = new(UserIndex.One);
    private readonly DispatcherTimer inputTimer;
    private readonly DateTime _creationTime = DateTime.Now;

    public InfoDialog()
    {
        InitializeComponent();

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
                inputTimer.Stop();
                this.Close();
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        inputTimer.Stop();
        this.Close();
    }
}
