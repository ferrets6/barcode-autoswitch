using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;
using BarcodeAutoSwitch.Infrastructure;
using BarcodeAutoSwitch.UI.ViewModels;
using BarcodeAutoSwitch.Windows;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Extensions.Configuration;
using System.IO;
using WpfApp = System.Windows.Application;
using WpfStartupEventArgs = System.Windows.StartupEventArgs;

namespace BarcodeAutoSwitch;

public partial class App : WpfApp
{
    // Exposed so MainWindow code-behind can read them without a DI container
    public static string AdriaticaPressVenditaUrl = string.Empty;
    public static string AdriaticaPressLoginUrl   = string.Empty;

    protected override void OnStartup(WpfStartupEventArgs e)
    {
        base.OnStartup(e);

        // Redirect Console.Write* to the WPF debug log window
        AppLogger.Initialize();

        InitialiseCef();

        // ── Build configuration ───────────────────────────────────────────────
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json",      optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true,  reloadOnChange: false)
            .Build();

        // ── Compose object graph (manual DI) ─────────────────────────────────
        var appSettings    = new AppSettings(config);
        var windowSwitcher = new WindowSwitcher();
        var keyboardSender = new KeyboardSender();
        var barcodeParser  = new BarcodeParser();
        var barcodeRouter  = new BarcodeRouter(new IRoutingStrategy[]
        {
            new NewspaperRoutingStrategy(),
            new DefaultRoutingStrategy()
        });

        AdriaticaPressVenditaUrl = appSettings.AdriaticaPressVenditaUrl;
        AdriaticaPressLoginUrl   = appSettings.AdriaticaPressLoginUrl;

        IBarcodeInputService ServiceFactory(BarcodeDeviceType t) => CreateInputService(t);

        var viewModel  = new MainViewModel(barcodeParser, barcodeRouter,
                                           windowSwitcher, keyboardSender, appSettings,
                                           ServiceFactory);
        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();
    }

    private static IBarcodeInputService CreateInputService(BarcodeDeviceType type) =>
        type == BarcodeDeviceType.UsbHid
            ? new RawInputBarcodeService()
            : new SerialPortService();

    private static void InitialiseCef()
    {
        CefSharpSettings.SubprocessExitIfParentProcessClosed = true;

        var settings = new CefSettings
        {
            CachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BarcodeAutoSwitch", "CefCache")
        };
        settings.CefCommandLineArgs.Add("enable-media-stream", "1");
        settings.CefCommandLineArgs.Add("disable-gpu", "1");

        Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
    }
}
