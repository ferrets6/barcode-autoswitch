using BarcodeAutoSwitch.Properties;
using BarcodeAutoSwitch.Utils;
using CefSharp;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BarcodeAutoSwitch
{
    public partial class MainWindow : Window
    {
        public string isAutoSwitchEnabled
        {
            get
            {
                return !disableAppSwitch ? "Abilitato" : "Disabilitato";
            }
        }

        public static SerialPort _port;
        public static SerialPort Port { get { return _port; } set { _port = value; } }

        public static string selectedSerialPort;
        private string newLineChars = "\r\n";

        private bool disableAppSwitch = false;
        private string enableDisableToggleString = "111111111100000011111111";
        private bool _sendToKeyboard;

        private static string _AdriaticaPressVenditaURL;
        private static string _AdriaticaPressLoginURL;
        private static string _AdriaticaPressAfterLoginURL;

        private ComPortWindow comPortWindow;

        public MainWindow()
        {
            try
            {
                WindowControl.AvviaNegozioFacile();
            }
            catch { }
            InitializeComponent();
            WindowTitle = Assembly.GetEntryAssembly().GetName().Name + " v" + Assembly.GetEntryAssembly().GetName().Version;
            browserGrid.Visibility = Visibility.Visible;
            backgroundGrid.Visibility = Visibility.Hidden;
            Win32.ShowHideConsole();

            // read configuration
            _AdriaticaPressVenditaURL = ConfigurationManager.AppSettings["AdriaticaPressVenditaURL"];
            _AdriaticaPressAfterLoginURL = ConfigurationManager.AppSettings["AdriaticaPressAfterLoginURL"];
            _AdriaticaPressLoginURL = ConfigurationManager.AppSettings["AdriaticaPressLoginURL"];
            selectedSerialPort = Settings.Default.SelectedSerialPort;

            Browser.Address = _AdriaticaPressVenditaURL;
            Browser.LoadingStateChanged += OnLoadingStateChanged;

            Port = new SerialPort(selectedSerialPort, 9600, Parity.None, 8, StopBits.One);

            Console.WriteLine("Application start...");

            // Attach a method to be called when there is data waiting in the port's buffer 
            Port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            Port.ErrorReceived += new SerialErrorReceivedEventHandler(serialErrorReceived);
            Port.Handshake = Handshake.RequestToSend;
            Port.NewLine = newLineChars;

            // Begin communications
            try
            {
                Port.Open();
            }
            catch (Exception e)
            {
                browserGrid.Visibility = Visibility.Hidden;
                backgroundGrid.Visibility = Visibility.Visible;
            }

            Console.WriteLine("Auto Switch Windows: abilitato");
        }

        public String WindowTitle
        {
            get { return (String)GetValue(MainWindow.TitleProperty); }
            set
            {
                SetValue(MainWindow.TitleProperty, value);
            }
        }

        private void serialErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            browserGrid.Visibility = Visibility.Hidden;
            backgroundGrid.Visibility = Visibility.Visible;
        }

        [STAThread]
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_port.IsOpen)
            {
                // Show all the incoming data in the port's buffer
                string readStr = _port.ReadLine();

                // check toggle enable/disable string
                if (string.Equals(readStr.Substring(1), enableDisableToggleString))
                {
                    _EnableDisableAutoSwitching();
                }
                else
                {
                    if (!disableAppSwitch)
                    {

                        // code identifier
                        /*
                         * A are EAN 8 code
                         * M are ISSN 13 + 5 code (for newspapers and co.)
                         * B are EAN 13 code
                         * N are Intervaled 2 of 5 codes (gratta e vinci)
                        */

                        char codeIdentifier = readStr[0];
                        // remove code identifier before send

                        if (readStr.Substring(1, 3) == "977" && ((codeIdentifier == 'M') || (codeIdentifier == 'B')))
                        {
                            // è un giornale
                            _sendToKeyboard = WindowControl.SelezionaAdriaticaPress();
                            Dispatcher.Invoke((Action)delegate
                            {
                                this.Focus();
                                _sendToKeyboard = Browser.Focus();
                                //while (!this.IsActive)
                                //{
                                //    if (this.IsActive)
                                //        break;
                                //}
                            });

                            SendKeys.SendWait("%T");
                        }
                        else
                        {
                            // tutto il resto va su NegozioFacile
                            _sendToKeyboard = WindowControl.SelezionaNegozioFacile();
                        }
                    }
                    else
                    {
                        _sendToKeyboard = true;
                    }
                    string codeToSend = readStr.Substring(1);
                    if (_sendToKeyboard)
                    {
                        SendKeys.SendWait(codeToSend);
                        SendKeys.SendWait("{ENTER}");
                    }
                }
                //Console.WriteLine(readStr);
            }
        }

        [STAThread]
        public void ChangeCOMPortButton(object sender, RoutedEventArgs e)
        {
            if (Port.IsOpen)
                Port.Close();
            comPortWindow = new ComPortWindow();
            comPortWindow.Closed += OnComPortWindowsClosed;
            comPortWindow.ShowDialog();
            Port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
        }
        public void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
        {
            if (!args.IsLoading)
            {
                string address = "";
                // Page has finished loading, do whatever you want here
                Dispatcher.Invoke(() => address = Browser.Address);
                if (!address.Contains(_AdriaticaPressLoginURL) && address != _AdriaticaPressVenditaURL)
                {
                    Dispatcher.Invoke(() => Browser.Address = _AdriaticaPressVenditaURL);
                }
            }
        }
        public void btnShowHideConsole_click(object sender, RoutedEventArgs e)
        {
            Win32.ShowHideConsole();
        }

        public void btnEnableDisable_click(object sender, RoutedEventArgs e)
        {
           _EnableDisableAutoSwitching();
        }

        private void _EnableDisableAutoSwitching()
        {

            disableAppSwitch = !disableAppSwitch;
            switch (!disableAppSwitch)
            {
                case true:
                    // è attivo
                    Dispatcher.Invoke((Action)delegate
                    {
                        statusLabel.Text = "Attivo";
                        statusLabelContainer.Background = Brushes.Green;
                        btnEnableDisable.Content = "Disattiva";
                    });
                    break;
                case false:
                    Dispatcher.Invoke((Action)delegate
                    {
                        statusLabel.Text = "Non attivo";
                        statusLabelContainer.Background = Brushes.Red;
                        btnEnableDisable.Content = "Attiva";
                    });
                    break;
            }

            _sendToKeyboard = false;
            string statusLabelText = !disableAppSwitch ? "Stato: Attivato" : "Stato: Disattivato";
            Console.WriteLine("Auto Switch Windows: {0}", !disableAppSwitch ? "abilitato" : "disabilitato");
        }

        public void OnComPortWindowsClosed(object sender, EventArgs e)
        {

            selectedSerialPort = comPortWindow.SelectedPortName;
            Settings.Default.SelectedSerialPort = selectedSerialPort;
            Settings.Default.Save();
            if (COMPort.OpenPortByName(selectedSerialPort, out _port))
            {
                browserGrid.Visibility = Visibility.Visible;
                backgroundGrid.Visibility = Visibility.Hidden;
                _port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            }

        }
    }

    internal sealed class Win32
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern bool DeleteMenu(IntPtr hWnd, uint uPosition, uint uFlags);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();

        private static uint MF_BYCOMMAND = 0x00000000;
        private static uint SC_CLOSE = 0xF060;

        private static bool _isConsoleVisible = true;
        public static void ShowHideConsole()
        {
            IntPtr hWnd = GetConsoleWindow();

            if (hWnd != IntPtr.Zero)
            {
                // disabilita il tasto per chiudere la console, altrimenti termina tutto l'applicativo
                IntPtr hMenu = GetSystemMenu(hWnd, false);
                if (hMenu != null) DeleteMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);

                if (_isConsoleVisible)
                {
                    ShowWindow(hWnd, 0); // 0 = SW_HIDE
                }
                else
                {
                    ShowWindow(hWnd, 9); // 9 = SW_RESTORE
                }

                _isConsoleVisible = !_isConsoleVisible;
            }
        }
    }
}
