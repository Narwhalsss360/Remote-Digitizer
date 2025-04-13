using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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

namespace Remote_Digitizer_Client
{
    /// <summary>
    /// Interaction logic for AllowFromWindow.xaml
    /// </summary>
    public partial class AllowFromWindow : Window
    {
        public class AddressContainer
        {
            public string Address { get; set; } = string.Empty;
        }

        public static readonly DependencyProperty AddressesProperty = DependencyProperty.Register(
            nameof(Addresses),
            typeof(AddressContainer[]),
            typeof(AllowFromWindow),
            new(new AddressContainer[0])
        );

        public AddressContainer[] Addresses
        {
            get => (AddressContainer[])GetValue(AddressesProperty);
            set => SetValue(AddressesProperty, value);
        }

        private IPAddress[] _saveIPs;

        public IPAddress[] IPAddresses
        {
            get => _saveIPs;
        }

        public AllowFromWindow(IPAddress[] currentList)
        {
            InitializeComponent();
            DataContext = this;
            _saveIPs = currentList;
            Addresses = (from address in currentList select new AddressContainer() { Address = address.ToString() }).ToArray();
        }

        private void RemoveClick(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button)!;
            Addresses = (
                from address in Addresses
                where address != button.DataContext
                select address
            ).ToArray();
        }

        private void AddClick(object sender, RoutedEventArgs e)
        {
            if (!IPAddress.TryParse(AddTextBox.Text, out IPAddress? address) || address is null)
            {
                MessageBox.Show($"{AddTextBox.Text} is not a valid IP Address", "Invalid IP Address", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string addressString = address.ToString();
            bool found = (from addr in Addresses
                         where addr.Address == addressString
                         select addr).Count() > 0;
            if (found)
            {
                MessageBox.Show($"{AddTextBox.Text} is already allowed", "Already Allowed", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            AddTextBox.Text = "";

            Addresses = [
                ..Addresses,
                new() { Address = addressString }
            ];
        }

        private void ApplyAndClose(object sender, EventArgs e)
        {
            _saveIPs = (from address in Addresses select IPAddress.Parse(address.Address)).ToArray();
            Close();
        }
    }
}
