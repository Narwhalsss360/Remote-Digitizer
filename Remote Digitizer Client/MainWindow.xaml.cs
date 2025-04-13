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

    static readonly string ALLOW_FROM_IPS_FILE_NAME = "AllowFrom.json";

    static readonly FileInfo ALLOW_FROM_IPS_FILE = new FileInfo(
        System.IO.Path.Join(
            AppDomain.CurrentDomain.BaseDirectory, ALLOW_FROM_IPS_FILE_NAME
        )
    );

    Thread _listenerThread;

    bool _listening = false;

    IPAddress[] _allowFrom = [];

    List<Profile> _profiles = new();

    int _currentProfileIndex = 0;

    Profile CurrentProfile { get => _profiles.ElementAt(_currentProfileIndex); }

    public MainWindow()
    {
        _listenerThread = new(() => { });
        InitializeComponent();
        LoadProfiles();
        LoadAllowFromIPs();
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
        _profiles = JsonSerializer.Deserialize<List<Profile>>(profilesFileStream) ?? new() { new() };
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

    private void LoadAllowFromIPs()
    {
        if (!ALLOW_FROM_IPS_FILE.Exists)
        {
            _profiles.Add(new());
            return;
        }

        using FileStream allowFromIPsFileStream = ALLOW_FROM_IPS_FILE.OpenRead();
        string[] ipStrings = JsonSerializer.Deserialize<string[]>(allowFromIPsFileStream) ?? [];
        List<IPAddress> allowFrom = new();
        foreach (string ipString in ipStrings)
        {
            if (!IPAddress.TryParse(ipString, out IPAddress? ip) || ip is null)
            {
                MessageBox.Show($"{ipString} could not be parsed", "IP Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
                continue;
            }
            allowFrom.Add(ip);
        }
        _allowFrom = allowFrom.ToArray();
    }

    private void SaveAllowFromIPs()
    {
        using FileStream allowFromIPsFileStream = ALLOW_FROM_IPS_FILE.OpenWrite();
        using StreamWriter writer = new(allowFromIPsFileStream);
        writer.Write(
            JsonSerializer.Serialize(from ip in _allowFrom select ip.ToString(), new JsonSerializerOptions() { WriteIndented = true })
        );
    }

    private void OpenAllowFromSettings(object sender, EventArgs e)
    {
        Button openButton = (sender as Button)!;
        AllowFromWindow allowFromWindow = new(_allowFrom);
        allowFromWindow.Closed += (sender, e) =>
        {
            _allowFrom = allowFromWindow.IPAddresses;
            openButton.IsEnabled = true;
            SaveAllowFromIPs();
        };
        openButton.IsEnabled = false;
        allowFromWindow.Show();
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

    private void AddProfileClick(object sender, EventArgs e)
    {
        _currentProfileIndex = _profiles.Count;
        _profiles.Add(new());
        ApplyCurrentProfile();
    }

    private void DeleteProfileClick(object sender, EventArgs e)
    {
        if (_profiles.Count == 1)
        {
            _profiles.Clear();
            _profiles.Add(new());
        }
        else
        {
            _profiles.Remove(CurrentProfile);
            _currentProfileIndex = 0;
        }
        ApplyCurrentProfile();
    }

    private void ApplyProfileName(object sender, EventArgs e)
    {
        bool found = (from profile in _profiles where profile.Name == ProfileComboBox.Text select Name).Count() > 0;
        if (found)
            return;
        CurrentProfile.Name = ProfileComboBox.Text;
        ProfileComboBox.ItemsSource = from profile in _profiles select profile.Name;
    }

    void ToggleListen(object sender, EventArgs e)
    {
        if (!_listening)
        {
            _listening = true;
            _listenerThread = new(Listener) { Name = "Remote Digitizer: Listener" };
            _listenerThread.Start();
            ConnectionStatusTextBlock.Text = "Listening & Waiting...";
            ListenToggleButtonText.Text = "Stop Listening";
        }
        else
        {
            _listening = false;
            ConnectionStatusTextBlock.Text = "Standby";
            ListenToggleButtonText.Text = "Listen";
        }
    }

    [DllImport("User32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    private void Listener()
    {
        using UdpClient listener = new(Constants.PORT);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, Constants.PORT);

        StylusUpdateMessage message, oldMessage = new();
        while (_listening)
        {
            if (listener.Available == 0)
                continue;

            byte[] data = listener.Receive(ref sender);
            string senderAddress = sender.Address.ToString();

            if (!(_allowFrom ?? []).Contains(sender.Address))
            {
                ConnectionStatusTextBlock.Dispatcher.InvokeAsync(() => ConnectionStatusTextBlock.Text = $"Received from ${sender} whose IP is not allowed.");
                continue;
            }

            if (data.Length != StylusUpdateMessage.BUFFER_SIZE)
                continue;
            
            ConnectionStatusTextBlock.Dispatcher.InvokeAsync(() => ConnectionStatusTextBlock.Text = $"Received from ${senderAddress}");
            message = new StylusUpdateMessage(data);

            double windowsScalingDivisor = SystemParameters.BorderWidth;
            double screenWidth = SystemParameters.WorkArea.Width / windowsScalingDivisor;
            double screenHeight = SystemParameters.WorkArea.Height / windowsScalingDivisor;

            SetCursorPos((int)(message.PositionX * screenWidth), (int)(message.PositionY * screenHeight));
            if ((message.Alternate != oldMessage.Alternate && !message.Alternate) || (message.Inverted != oldMessage.Inverted && !message.Inverted))
                EnteredNormallyMap.Play(MouseButtonState.Pressed);
            if (message.Alternate != oldMessage.Alternate)
                EnteredAlternateMap.Play(message.Alternate ? MouseButtonState.Pressed : MouseButtonState.Released);
            if (message.Inverted != oldMessage.Inverted)
                EnteredInvertedMap.Play(message.Inverted ? MouseButtonState.Pressed : MouseButtonState.Released);
            if (message.Touched != oldMessage.Touched)
                TouchMap.Play(message.Touched ? MouseButtonState.Pressed : MouseButtonState.Released);

            oldMessage = message;
            PositionTextBox.Dispatcher.InvokeAsync(() => PositionTextBox.Text =
                $"({message.PositionX:0.000}, {message.PositionY:0.000})"
            );
            StatusTextBox.Dispatcher.InvokeAsync(() => StatusTextBox.Text =
                $"{(message.Alternate ? "Alternate " : "")}{(message.Touched ? "Drawing" : "Not Drawing")}{(message.Inverted ? " Inverted" : "")}"
            );
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        SaveProfiles();
        if (_listening)
        {
            ToggleListen(sender ?? new(), e);
            _listenerThread.Join();
        }
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