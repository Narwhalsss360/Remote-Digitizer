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

    private void Receiver()
    {
        UdpClient listener = new(Constants.PORT);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, Constants.PORT);

        const int MAX_WAITING_ITERATIONS = 150;
        const int WAIT_TIME_BETWEEN_LONG_LISTEN = 2;

        ConnectionStatusTextBlock.Dispatcher.Invoke(() => ConnectionStatusTextBlock.Text = "Listening & Waiting...");
        int i;
        while (!_stopReceiver)
        {
            for (i = 0; i < MAX_WAITING_ITERATIONS; i++)
            {
                if (listener.Available > 0 || _stopReceiver)
                    break;
            }

            if (_stopReceiver)
                break;

            if (i == MAX_WAITING_ITERATIONS)
            {
                Thread.Sleep(WAIT_TIME_BETWEEN_LONG_LISTEN);
                continue;
            }

            byte[] data = listener.Receive(ref sender);

            if (data.Length != StylusUpdateMessage.BUFFER_SIZE)
                continue;

            string senderAddress = sender.Address.ToString();
            ConnectionStatusTextBlock.Dispatcher.Invoke(() => ConnectionStatusTextBlock.Text = $"Received from ${senderAddress}");

            StylusUpdateMessage message;
            if (_allowFrom is null)
                message = new StylusUpdateMessage(data);
            else if (_allowFrom.Contains(sender.Address))
                message = new StylusUpdateMessage(data);
            else
                continue;

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