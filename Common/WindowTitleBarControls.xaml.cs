using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Common
{
    /// <summary>
    /// Interaction logic for WindowTitleBarControls.xaml
    /// </summary>
    public partial class WindowTitleBarControls : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(WindowTitleBarControls),
            new PropertyMetadata("Window")
        );

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly RoutedEvent MinimizeEvent = EventManager.RegisterRoutedEvent(
            nameof(Minimize),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(WindowTitleBarControls)
        );

        public static readonly RoutedEvent MaximizeEvent = EventManager.RegisterRoutedEvent(
            nameof(Maximize),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(WindowTitleBarControls)
        );

        public static readonly RoutedEvent CloseEvent = EventManager.RegisterRoutedEvent(
            nameof(Close),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(WindowTitleBarControls)
        );

        public event RoutedEventHandler Minimize
        {
            add { AddHandler(MinimizeEvent, value); }
            remove { RemoveHandler(MinimizeEvent, value); }
        }

        public event RoutedEventHandler Maximize
        {
            add { AddHandler(MaximizeEvent, value); }
            remove { RemoveHandler(MaximizeEvent, value); }
        }

        public event RoutedEventHandler Close
        {
            add { AddHandler(CloseEvent, value); }
            remove { RemoveHandler(CloseEvent, value); }
        }

        private Color _lastColor;

        public WindowTitleBarControls()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void ButtonClick(object sender, MouseEventArgs e)
        {
            if (sender is not Border border)
                return;

            if (sender == MinimizeButton)
                RaiseEvent(new(MinimizeEvent, e.Source));
            else if (sender == MaximizeButton)
                RaiseEvent(new(MaximizeEvent, e.Source));
            else if (sender == CloseButton)
                RaiseEvent(new(CloseEvent, e.Source));
        }

        private void ButtonMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Border border)
                return;
            if (border.Background is not SolidColorBrush solid)
                return;
            _lastColor = solid.Color;
            border.Background = new SolidColorBrush(Color.Multiply(solid.Color, .75f));
        }

        private void ButtonMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not Border border)
                return;
            border.Background = new SolidColorBrush(_lastColor);
        }
    }
}
