using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BarcodeAutoSwitch.Utils
{
    class COMPort
    {
        private static string newLineChars = "\r\n";
        public static bool OpenPortByName(string portName, out SerialPort port) {
            try
            {
                port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                port.Handshake = Handshake.RequestToSend;
                port.NewLine = newLineChars;
                port.Open();
                return true;
            }
           catch (Exception ex) {
                port = new SerialPort();
                return false;
            }
        }
    }
}
