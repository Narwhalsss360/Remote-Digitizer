using Common;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Net;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsInput;
using MouseButton = System.Windows.Input.MouseButton;
using System.Runtime.InteropServices;

namespace Remote_Digitizer_Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    Thread _receiverThread;

    bool _stopReceiver = false;

    List<IPAddress>? _allowFrom;

    public MainWindow()
    {
        _receiverThread = new(Receiver);
        InitializeComponent();
        TitleBarControls.SizeChanged += (sender, e) => TitleBarControls.Clip = new RectangleGeometry(
            new Rect(new Size(Width, Height)),
            MainBorder.CornerRadius.TopLeft,
            MainBorder.CornerRadius.TopRight
        );
        TitleBarControls.MouseDown += TitleBarToggleDragMove;
        TitleBarControls.MouseUp += TitleBarToggleDragMove;
        StatusBarPanel.SizeChanged += (sender, e) => StatusBarPanel.Clip = new RectangleGeometry(
            new Rect(0, -MainBorder.CornerRadius.TopLeft, MainBorder.ActualWidth, StatusBarPanel.ActualHeight + MainBorder.CornerRadius.TopLeft),
            MainBorder.CornerRadius.TopLeft,
            MainBorder.CornerRadius.TopRight
        );
        Closing += MainWindow_Closing;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _receiverThread.Start();
    }

    [DllImport("User32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    private void Receiver()
    {
        UdpClient listener = new(Constants.PORT);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, Constants.PORT);

        ConnectionStatusTextBlock.Dispatcher.Invoke(() => ConnectionStatusTextBlock.Text = "Listening & Waiting...");
        int i;
        StylusUpdateMessage message, oldMessage = new();
        while (!_stopReceiver)
        {
            byte[] data = listener.Receive(ref sender);

            if (data.Length != StylusUpdateMessage.BUFFER_SIZE)
                continue;

            string senderAddress = sender.Address.ToString();
            ConnectionStatusTextBlock.Dispatcher.Invoke(() => ConnectionStatusTextBlock.Text = $"Received from ${senderAddress}");

            if (_allowFrom is null)
                message = new StylusUpdateMessage(data);
            else if (_allowFrom.Contains(sender.Address))
                message = new StylusUpdateMessage(data);
            else
                continue;


            SetCursorPos((int)(message.PositionX * 1920), (int)(message.PositionY * 1080));
            if ((message.Alternate != oldMessage.Alternate && !message.Alternate) || (message.Inverted != oldMessage.Inverted && !message.Inverted))
                EnteredNormallyMap.Play(MouseButtonState.Pressed);
            if (message.Alternate != oldMessage.Alternate)
                EnteredAlternateMap.Play(message.Alternate ? MouseButtonState.Pressed : MouseButtonState.Released);
            if (message.Inverted != oldMessage.Inverted)
                EnteredInvertedMap.Play(message.Inverted ? MouseButtonState.Pressed : MouseButtonState.Released);
            if (message.Touched != oldMessage.Touched)
                TouchMap.Play(message.Touched ? MouseButtonState.Pressed : MouseButtonState.Released);

            oldMessage = message;
            PositionTextBox.Dispatcher.Invoke(() => PositionTextBox.Text =
                $"({message.PositionX:0.000}, {message.PositionY:0.000})"
            );
            StatusTextBox.Dispatcher.Invoke(() => StatusTextBox.Text =
                $"{(message.Alternate ? "Alternate " : "")}{(message.Touched ? "Drawing" : "Not Drawing")}{(message.Inverted ? " Inverted" : "")}"
            );
        }

        listener.Close();
    }

    private void MainWindow_Closing(object? sender, EventArgs e)
    {
        _stopReceiver = true;
        _receiverThread.Join();
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