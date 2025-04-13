using Common;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Remote_Digitizer_Server;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static StylusUpdateMessage StylusState = new();

    BroadcastWindow? _broadcaster;

    static MainWindow _instance;

    private static readonly TimeSpan UI_UPDATE_INTERVAL = TimeSpan.FromMilliseconds(1000 / 30);

    static DateTime _lastUpdate = DateTime.Now;

    public MainWindow()
    {
        _instance = this;
        InitializeComponent();
        TitleBarControls.SizeChanged += (sender, e) => TitleBarControls.Clip = new RectangleGeometry(
            new Rect(new Size(Width, Height)),
            MainBorder.CornerRadius.TopLeft,
            MainBorder.CornerRadius.TopRight
        );
        TitleBarControls.MouseDown += TitleBarToggleDragMove;
        TitleBarControls.MouseUp += TitleBarToggleDragMove;
        Closed += (sender, e) => _broadcaster?.Close();
    }

    public static void UpdateStateOnUI()
    {
        if (DateTime.Now - _lastUpdate < UI_UPDATE_INTERVAL)
            return;
        _lastUpdate = DateTime.Now;
        _instance.Dispatcher.InvokeAsync(() =>
        {
            _instance.PositionTextBlock.Text = $"({StylusState.PositionX:0.000}, {StylusState.PositionY:0.000})";
            _instance.StateTextBlock.Text = $"{(StylusState.Alternate ? "Alternate " : "")}{(StylusState.Touched ? "Drawing" : "Not Drawing")}{(StylusState.Inverted ? " Inverted" : "")}";
        });
    }

    private void SetBlankState()
    {
        PositionTextBlock.Text = "(---, ---)";
        StateTextBlock.Text = "Not Drawing";
    }

    private void StartBroadcast(object sender, EventArgs e)
    {
        if (!IPAddress.TryParse(IPEntry.Text, out IPAddress? ip) || ip is null)
        {
            MessageBox.Show($"{IPEntry.Text} is not a valid IP Address.", "Invalid IP Address", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Button button = (sender as Button)!;
        button.IsEnabled = false;
        IPEntry.IsEnabled = false;

        _broadcaster = new(ip);
        _broadcaster.Closed += (sender, e) =>
        {
            button.IsEnabled = true;
            IPEntry.IsEnabled = true;
            SetBlankState();
        };
        _broadcaster.Show();
    }

    private void TitleBarToggleDragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void WindowTitleBarControls_Minimize(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void WindowTitleBarControls_Maximize(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void WindowTitleBarControls_Close(object sender, RoutedEventArgs e)
    {
        Close();
    }
}