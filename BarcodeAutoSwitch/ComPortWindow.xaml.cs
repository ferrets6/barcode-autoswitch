using BarcodeAutoSwitch.Properties;
using BarcodeAutoSwitch.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
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
using System.Windows.Shapes;

namespace BarcodeAutoSwitch
{
    /// <summary>
    /// Interaction logic for ComPortWindow.xaml
    /// </summary>
    public partial class ComPortWindow : Window
    {
        private SerialPort _tmpSerialPort;
        public string SelectedPortName { get { return _tmpSelectedPort; } }
        private bool avoidEvents = false;
        private string _tmpSelectedPort;
        private string checkPortStringValue = "111111111122222200000000";

        public ComPortWindow()
        {
            InitializeComponent();
            portComListComboBox.ItemsSource = SerialPort.GetPortNames();
            _tmpSelectedPort = MainWindow.selectedSerialPort;
            avoidEvents = true;
            portComListComboBox.SelectedValue = _tmpSelectedPort;
            avoidEvents = false;
            var bitmap = new BitmapImage(new Uri(@"/Resources/checkPortBarcode.gif", UriKind.Relative));
            checkPortImage.Source = bitmap;

            COMPort.OpenPortByName(_tmpSelectedPort, out _tmpSerialPort);
            _tmpSerialPort.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
        }

        private void portComListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!avoidEvents)
            {
                //MainWindow.selectedSerialPort = portComListComboBox.SelectedItem > -1;
                try
                {
                    if (_tmpSerialPort != null && _tmpSerialPort.IsOpen)
                        _tmpSerialPort.Close();
                    this.Dispatcher.Invoke(() =>
                    {
                        checkPortImage.Source = new BitmapImage(new Uri(@"/Resources/checkPortBarcode.gif", UriKind.Relative));
                    });

                    _tmpSelectedPort = portComListComboBox.SelectedValue.ToString();
                    COMPort.OpenPortByName(_tmpSelectedPort, out _tmpSerialPort);
                    _tmpSerialPort.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
                }
                catch (Exception ex)
                {

                }
            }
        }

        [STAThread]
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_tmpSerialPort.IsOpen)
            {
                // Show all the incoming data in the port's buffer
                string readStr = _tmpSerialPort.ReadLine();

                // check toggle enable/disable string
                if (string.Equals(readStr.Substring(1), checkPortStringValue))
                {

                    this.Dispatcher.Invoke(() =>
                    {
                        checkPortImage.Source = new BitmapImage(new Uri(@"/Resources/portOk.gif", UriKind.Relative));
                    });

                    _tmpSerialPort.Close();
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        checkPortImage.Source = new BitmapImage(new Uri(@"/Resources/portKo.gif", UriKind.Relative));
                    });
                }
            }
        }
    }
}
