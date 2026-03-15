using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Configuration;

namespace BarcodeAutoSwitch.Utils
{

    class WindowControl
    {
        private static string _NegozioFacileProcessName = ConfigurationManager.AppSettings["NegozioFacileProcessName"];
        //private static string _NegozioFacileProcessName = "notepad++";
        private static string _AdriaticaPressProcessName = "BarcodeAutoSwitch";

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        public static void AvviaNegozioFacile()
        {
            Process bProcess = Process.GetProcessesByName(_NegozioFacileProcessName).FirstOrDefault();
            if (bProcess == null) {
                // start the process
                //Process.Start(_NegozioFacileProcessName);
            }
        }

        public static bool SelezionaAdriaticaPress()
        {
            BringMainWindowToFront(_AdriaticaPressProcessName);
            return true;
        }
        public static bool SelezionaNegozioFacile()
        {
            BringMainWindowToFront(_NegozioFacileProcessName);
            return true;
        }

        public static void BringMainWindowToFront(string processName)
        {
            Process bProcess = Process.GetProcessesByName(processName).FirstOrDefault();

            // check if the process is running
            if (bProcess != null)
            {
                ShowWindow(bProcess.Handle, ShowWindowEnum.Minimize);
                // check if the window is hidden / minimized
                if (bProcess.MainWindowHandle == IntPtr.Zero)
                {
                    // the window is hidden so try to restore it before setting focus.
                    ShowWindow(bProcess.Handle, ShowWindowEnum.Restore);
                    Console.WriteLine("Process {0} was hidden.", processName);
                }

                // set user the focus to the window
                SetForegroundWindow(bProcess.MainWindowHandle);
                Console.WriteLine("Process {0} set to focus.", processName);
            }
            else
            {
                // the process is not running
                Console.WriteLine("Process {0} not found.", processName);
            }
        }
    }
}