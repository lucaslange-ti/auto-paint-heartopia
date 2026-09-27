using System.Diagnostics;
using System.IO;
using System.Windows;
using AutoPaint.App.Bridge;
using AutoPaint.Core.Config;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace AutoPaint.App;

public partial class MainWindow : Window
{
    private const string VirtualHost = "app.local";

    private readonly BridgeRouter _router;
    private readonly BridgeEventHub _events;
    private readonly IConfigStore _config;
    private WebViewBridge? _bridge;

    public MainWindow(BridgeRouter router, BridgeEventHub events, IConfigStore config)
    {
        _router = router;
        _events = events;
        _config = config;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _config.Load();

            var userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoPaintHeartopia", "webview2");
            Directory.CreateDirectory(userData);

            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
            await Web.EnsureCoreWebView2Async(env);

            var core = Web.CoreWebView2;
            core.SetVirtualHostNameToFolderMapping(
                VirtualHost,
                Path.Combine(AppContext.BaseDirectory, "wwwroot"),
                CoreWebView2HostResourceAccessKind.Deny);

#if !DEBUG
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
#endif
            core.Settings.IsStatusBarEnabled = false;

            // Links externos (target="_blank" ou navegação para fora do app) abrem
            // no navegador padrão, nunca dentro da janela do app.
            core.NewWindowRequested += (_, a) => { a.Handled = true; OpenExternal(a.Uri); };
            core.NavigationStarting += (_, a) =>
            {
                if (IsAppUri(a.Uri)) return;
                a.Cancel = true;
                OpenExternal(a.Uri);
            };

            _bridge = new WebViewBridge(core, _router, _events, Dispatcher);

            Web.Source = new Uri($"https://{VirtualHost}/index.html");
            Log.Information("WebView2 pronto (runtime {Version})", CoreWebView2Environment.GetAvailableBrowserVersionString());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Falha ao inicializar o WebView2");
            MessageBox.Show(
                "Não foi possível iniciar o componente de interface (WebView2).\n" +
                "Verifique se o WebView2 Runtime está instalado.",
                "Auto Paint Heartopia", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool IsAppUri(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var u) && u.Host.Equals(VirtualHost, StringComparison.OrdinalIgnoreCase);

    private static void OpenExternal(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var u) || (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp))
        {
            Log.Warning("Link externo ignorado: {Uri}", uri);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falha ao abrir link externo {Uri}", uri);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _bridge?.Dispose();
        _config.Flush();
        base.OnClosed(e);
    }
}
