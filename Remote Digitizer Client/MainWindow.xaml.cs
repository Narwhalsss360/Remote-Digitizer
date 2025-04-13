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
using System.IO;
using System.Text.Json;
using System.ComponentModel;

namespace Remote_Digitizer_Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    static readonly string PROFILES_FILE_NAME = "Profiles.json";

    static readonly FileInfo PROFILES_FILE = new FileInfo(
        System.IO.Path.Join(
            AppDomain.CurrentDomain.BaseDirectory, PROFILES_FILE_NAME
        )
    );

    Thread _receiverThread;

    bool _stopReceiver = false;

    List<IPAddress>? _allowFrom;

    HashSet<Profile> _profiles = new();

    int _currentProfileIndex = 0;

    Profile CurrentProfile { get => _profiles.ElementAt(_currentProfileIndex); }

    public MainWindow()
    {
        _receiverThread = new(Receiver);
        InitializeComponent();
        LoadProfiles();
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

        EnteredNormallyMap.InputVerified += ApplyToProfile;
        EnteredAlternateMap.InputVerified += ApplyToProfile;
        EnteredInvertedMap.InputVerified += ApplyToProfile;
        TouchMap.InputVerified += ApplyToProfile;

        ProfileComboBox.LostFocus += ApplyProfileName;
        ProfileComboBox.KeyDown += (sender, e) => { if (e.Key == Key.Enter || e.Key == Key.Escape) ApplyProfileName(sender, e); };
    }

    private void LoadProfiles()
    {
        if (!PROFILES_FILE.Exists)
        {
            _profiles.Add(new());
            return;
        }

        using FileStream profilesFileStream = PROFILES_FILE.OpenRead();
        _profiles = JsonSerializer.Deserialize<HashSet<Profile>>(profilesFileStream) ?? new() { new() };
        ApplyCurrentProfile();
    }

    private void SaveProfiles()
    {
        using FileStream profilesFileStream = PROFILES_FILE.OpenWrite();
        using StreamWriter writer = new(profilesFileStream);
        writer.Write(
            JsonSerializer.Serialize(_profiles, new JsonSerializerOptions() { WriteIndented = true })
        );
    }
    private void ApplyToProfile(object? sender, EventArgs e)
    {
        CurrentProfile.EnteredNormally.InputType = EnteredNormallyMap.InputType;
        CurrentProfile.EnteredNormally.Input = EnteredNormallyMap.VerifiedInput;

        CurrentProfile.EnteredAlternate.InputType = EnteredAlternateMap.InputType;
        CurrentProfile.EnteredAlternate.Input = EnteredAlternateMap.VerifiedInput;

        CurrentProfile.EnteredInverted.InputType = EnteredInvertedMap.InputType;
        CurrentProfile.EnteredInverted.Input = EnteredInvertedMap.VerifiedInput;

        CurrentProfile.Touch.InputType = TouchMap.InputType;
        CurrentProfile.Touch.Input = TouchMap.VerifiedInput;
    }
    private void ApplyCurrentProfile()
    {
        EnteredNormallyMap.InputType = CurrentProfile.EnteredNormally.InputType;
        EnteredNormallyMap.VerifiedInput = CurrentProfile.EnteredNormally.Input;

        EnteredAlternateMap.InputType = CurrentProfile.EnteredAlternate.InputType;
        EnteredAlternateMap.VerifiedInput = CurrentProfile.EnteredAlternate.Input;

        EnteredInvertedMap.InputType = CurrentProfile.EnteredInverted.InputType;
        EnteredInvertedMap.VerifiedInput = CurrentProfile.EnteredInverted.Input;

        TouchMap.InputType = CurrentProfile.Touch.InputType;
        TouchMap.VerifiedInput = CurrentProfile.Touch.Input;

        ProfileComboBox.ItemsSource = from profile in _profiles select profile.Name;
        ProfileComboBox.SelectedIndex = _currentProfileIndex;
    }

    private void ApplyProfileName(object sender, EventArgs e)
    {
        bool found = (from profile in _profiles where profile.Name == ProfileComboBox.Text select Name).Count() > 0;
        if (found)
            return;
        CurrentProfile.Name = ProfileComboBox.Text;
        ProfileComboBox.ItemsSource = from profile in _profiles select profile.Name;
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

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _stopReceiver = true;
        SaveProfiles();
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