using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Remote_Digitizer_Server
{
    /// <summary>
    /// Interaction logic for BroadcastWindow.xaml
    /// </summary>
    public partial class BroadcastWindow : Window
    {
        private static readonly int BROADCAST_INTERVAL = 1000 / 144;

        public IPAddress IPAddress { get; private set; }

        public static readonly DependencyProperty StylusCanvasMarginProperty = DependencyProperty.Register(
            nameof(StylusCanvasMargin),
            typeof(Thickness),
            typeof(BroadcastWindow),
            new(new Thickness(5))
        );

        public Thickness StylusCanvasMargin
        {
            get => (Thickness)GetValue(StylusCanvasMarginProperty);
            set => SetValue(StylusCanvasMarginProperty, value);
        }

        public static readonly DependencyProperty DrawOnCanvasProperty = DependencyProperty.Register(
            nameof(DrawOnCanvas),
            typeof(bool),
            typeof(BroadcastWindow),
            new(false)
        );

        public bool DrawOnCanvas
        {
            get => (bool)GetValue(DrawOnCanvasProperty);
            set => SetValue(DrawOnCanvasProperty, value);
        }

        AutoResetEvent _dataReady = new(false);

        Thread _broadcasterThread;

        bool _broadcast = true;

        Guid _alternateGuid = Guid.Empty;

        public BroadcastWindow(IPAddress? ipAddress)
        {
            IPAddress = ipAddress ?? IPAddress.Broadcast;
            DataContext = this;
            _broadcasterThread = new(Broadcast);
            _broadcasterThread.Start();
            InitializeComponent();

            Closing += (sender, e) =>
            {
                _broadcast = false;
                _dataReady.Set();
                _broadcasterThread.Join();
            };

            CanvasFrame.StylusInRange += CanvasFrame_StylusInRange;
            CanvasFrame.StylusButtonDown += CanvasFrame_StylusButtonDown;
            CanvasFrame.StylusButtonUp += CanvasFrame_StylusButtonUp;
            CanvasFrame.StylusDown += CanvasFrame_StylusDown;
            CanvasFrame.StylusMove += CanvasFrame_StylusMove;
            CanvasFrame.StylusUp += CanvasFrame_StylusUp;
            CanvasFrame.StylusOutOfRange += CanvasFrame_StylusOutOfRange;
        }

        private void SavePosition(Point p)
        {
            MainWindow.StylusState.PositionX = p.X / CanvasFrame.ActualWidth;
            MainWindow.StylusState.PositionY = p.Y / CanvasFrame.ActualHeight;
        }

        private void CanvasFrame_StylusInRange(object sender, StylusEventArgs e)
        {
            SavePosition(e.GetPosition(CanvasFrame));
            MainWindow.StylusState.Inverted = e.Inverted;
            CanvasFrame.MouseMove += CanvasFrame_MouseMove;
            MainWindow.UpdateStateOnUI();
            _dataReady.Set();
        }

        private void CanvasFrame_StylusButtonDown(object sender, StylusButtonEventArgs e)
        {
            if (_alternateGuid == Guid.Empty)
            {
                if (e.StylusDevice.StylusButtons.Count > 1)
                {
                    _alternateGuid = e.StylusDevice.StylusButtons[1].Guid;
                }
                else
                {
                    _alternateGuid = Guid.NewGuid();
                    return;
                }
            }

            if (_alternateGuid != e.StylusButton.Guid)
                return;
            MainWindow.StylusState.Alternate = true;
            MainWindow.UpdateStateOnUI();
            _dataReady.Set();
        }

        private void CanvasFrame_StylusButtonUp(object sender, StylusButtonEventArgs e)
        {

            if (_alternateGuid != e.StylusButton.Guid)
                return;
            MainWindow.StylusState.Alternate = false;
            MainWindow.UpdateStateOnUI();
            _dataReady.Set();
        }

        private void CanvasFrame_MouseMove(object sender, MouseEventArgs e)
        {
            SavePosition(e.GetPosition(CanvasFrame));
            MainWindow.UpdateStateOnUI();
            _dataReady.Set();
        }

        private void CanvasFrame_StylusDown(object sender, StylusDownEventArgs e)
        {
            SavePosition(e.GetPosition(CanvasFrame));
            MainWindow.StylusState.Touched = true;
            MainWindow.StylusState.Inverted = e.Inverted;
            CanvasFrame.MouseMove -= CanvasFrame_MouseMove;
            MainWindow.UpdateStateOnUI();
            _dataReady.Set();
        }

        private void CanvasFrame_StylusMove(object sender, StylusEventArgs e)
        {
            SavePosition(e.GetPosition(CanvasFrame));
            MainWindow.UpdateStateOnUI();
            _dataReady.Set();
        }

        private void CanvasFrame_StylusUp(object sender, StylusEventArgs e)
        {
            MainWindow.StylusState.Touched = false;
            MainWindow.StylusState.Inverted = e.Inverted;
            CanvasFrame.MouseMove += CanvasFrame_MouseMove;
            MainWindow.UpdateStateOnUI();
            _dataReady.Set();
        }

        private void CanvasFrame_StylusOutOfRange(object sender, StylusEventArgs e)
        {
            CanvasFrame.MouseMove -= CanvasFrame_MouseMove;
            MainWindow.StylusState.Inverted = false;
            MainWindow.UpdateStateOnUI();
            _dataReady.Set();
        }

        private void Broadcast()
        {
            using Socket broadcastSocket = new(IPAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            broadcastSocket.EnableBroadcast = true;

            byte[] buffer = new byte[StylusUpdateMessage.BUFFER_SIZE];
            IPEndPoint endPoint = new(IPAddress, Constants.PORT);
            while (true)
            {
                _dataReady.WaitOne();
                _dataReady.Reset();
                if (!_broadcast)
                    break;
                broadcastSocket.SendTo(MainWindow.StylusState.Encode(buffer), endPoint);
            }
        }
    }
}
