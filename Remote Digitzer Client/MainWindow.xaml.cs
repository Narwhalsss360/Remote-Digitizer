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

namespace Remote_Digitzer_Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        TitleBarControls.SizeChanged += (sender, e) => TitleBarControls.Clip = new RectangleGeometry(
            new Rect(new Size(Width, Height)),
            MainBorder.CornerRadius.TopLeft,
            MainBorder.CornerRadius.TopRight
        );
        TitleBarControls.MouseDown += TitleBarToggleDragMove;
        TitleBarControls.MouseUp += TitleBarToggleDragMove;
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