using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using WindowsInput;
using WindowsInput.Native;

namespace Remote_Digitizer_Client
{
    /// <summary>
    /// Interaction logic for InputMapSelection.xaml
    /// </summary>
    public partial class InputMapSelection : UserControl
    {
        public enum StylusInputType
        {
            Nothing,
            Mouse,
            Key
        }

        public static readonly InputSimulator InputSimulator = new();

        public readonly char KEY_DELIMITER = '+';

        public static readonly Dictionary<string, VirtualKeyCode> MODIFIERS = new()
        {
            { "shift", VirtualKeyCode.SHIFT },
            { "ctrl", VirtualKeyCode.CONTROL },
            { "alt", VirtualKeyCode.MENU },
            { "win", VirtualKeyCode.LWIN },
            { "lwin", VirtualKeyCode.LWIN },
            { "rwin", VirtualKeyCode.RWIN },
            { "lshift", VirtualKeyCode.LSHIFT },
            { "rshift", VirtualKeyCode.RSHIFT },
            { "lctrl", VirtualKeyCode.LCONTROL },
            { "rctrl", VirtualKeyCode.RCONTROL },
            { "lalt", VirtualKeyCode.LMENU },
            { "ralt", VirtualKeyCode.RMENU }
        };

        StylusInputType _inputType = StylusInputType.Nothing;

        public StylusInputType InputType
        {
            get => _inputType;
            set => InputTypeCombo.Dispatcher.Invoke(() => InputTypeCombo.SelectedItem = value);
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(InputMapSelection),
            new(nameof(Title))
        );

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private string _verifiedInput = string.Empty;

        public string VerifiedInput
        {
            get => _verifiedInput;
            set
            {
                _verifiedInput = IsValidInput(value) ? value : string.Empty;
                InputTextBox.Dispatcher.Invoke(() => InputTextBox.Text = _verifiedInput);
            }
        }
        private bool _nextChangeIsLowered = false;

        public VirtualKeyCode[] Modifiers { get; private set; } = [];

        public string TextEntry { get; private set; } = string.Empty;

        public event EventHandler? InputVerified;

        public InputMapSelection()
        {
            DataContext = this;
            InitializeComponent();
            InputTextBox.LostFocus += (sender, e) => VerifyInput();
            InputTextBox.KeyDown += (sender, e) => { if (e.Key == Key.Enter || e.Key == Key.Escape) InputTypeCombo.Focus(); };
            InputTextBox.TextChanged += InputTextBox_TextChanged;
            InputTypeCombo.SelectionChanged += (sender, e) =>
            {
                InputTextBox.Text = "";
                _inputType = (StylusInputType)InputTypeCombo.SelectedItem;
                InputTextBox.IsEnabled = InputType != StylusInputType.Nothing;
            };
        }

        public void PlayKeys()
        {
            if (InputType != StylusInputType.Key)
                return;

            foreach (VirtualKeyCode modifier in Modifiers)
                InputSimulator.Keyboard.KeyDown(modifier);
            if (TextEntry != "")
                InputSimulator.Keyboard.TextEntry(TextEntry);
            foreach (VirtualKeyCode modifier in Modifiers.Reverse())
                InputSimulator.Keyboard.KeyDown(modifier);
        }

        public void PlayMouse(MouseButtonState state)
        {
            if (_verifiedInput == "")
                return;

            if (state == MouseButtonState.Pressed)
            {
                if (_verifiedInput == "left")
                    InputSimulator.Mouse.LeftButtonDown();
                else
                    InputSimulator.Mouse.RightButtonDown();
            }
            else
            {
                if (_verifiedInput == "left")
                    InputSimulator.Mouse.LeftButtonUp();
                else
                    InputSimulator.Mouse.RightButtonUp();
            }
        }

        public void Play(MouseButtonState state)
        {
            switch (InputType)
            {
                case StylusInputType.Nothing:
                    break;
                case StylusInputType.Mouse:
                    PlayMouse(state);
                    break;
                case StylusInputType.Key:
                    if (state == MouseButtonState.Pressed)
                        PlayKeys();
                    break;
                default:
                    break;
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_nextChangeIsLowered)
            {
                _nextChangeIsLowered = false;
                return;
            }
            InputTextBox.Text = InputTextBox.Text.ToLower();
        }

        private bool IsValidInput(string input)
        {
            switch (InputType)
            {
                case StylusInputType.Mouse:
                    return input == "left" || input == "right";
                case StylusInputType.Key:
                    string[] deliminated = Deliminate(input);
                    List<VirtualKeyCode> modifierCodes = new();
                    Modifiers = [];
                    TextEntry = string.Empty;

                    foreach (string s in deliminated)
                    {
                        if (MODIFIERS.ContainsKey(s))
                        {
                            modifierCodes.Add(MODIFIERS[s]);
                            continue;
                        }
                        TextEntry += s;
                    }

                    Modifiers = modifierCodes.ToArray();
                    return true;
                case StylusInputType.Nothing:
                    return true;
                default:
                    return false;
            }
        }

        private void VerifyInput()
        {
            if (IsValidInput(InputTextBox.Text))
            {
                _verifiedInput = InputTextBox.Text;
                InputTextBox.Foreground = Brushes.Black;
                InputVerified?.Invoke(this, new());
                return;
            }
            InputTextBox.Foreground = Brushes.Red;
            _verifiedInput = "";
        }

        private string[] Deliminate(string input)
        {
            List<string> keys = new() { "" };
            bool lastWasDelimiter = false;

            foreach (char c in input)
            {
                if (c == KEY_DELIMITER && !lastWasDelimiter)
                {
                    lastWasDelimiter = true;
                    keys.Add("");
                    continue;
                }
                else if (lastWasDelimiter)
                {
                    lastWasDelimiter = false;
                }

                keys[^1] += c;
            }

            if (keys.Count == 1 && keys[0] == "")
                return [];

            return keys.ToArray();
        }
    }
}
