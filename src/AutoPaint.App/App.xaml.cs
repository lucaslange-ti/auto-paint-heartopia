using System.IO;
using System.Windows;
using System.Windows.Threading;
using AutoPaint.App.Composition;
using AutoPaint.App.Updates;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AutoPaint.App;

public partial class App : Application
{
    private IServiceProvider _services = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoPaintHeartopia");
        Directory.CreateDirectory(appData);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(appData, "logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
#if DEBUG
            .WriteTo.Console()
#endif
            .CreateLogger();

        Log.Information("Auto Paint Heartopia iniciando");

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Fatal(args.ExceptionObject as Exception, "UnhandledException");
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "UnobservedTaskException");
            args.SetObserved();
        };

        _services = ServiceRegistration.Build();

        var window = _services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();

        // Checagem de atualização em segundo plano (no-op em build portátil/dev).
        _ = _services.GetRequiredService<UpdateService>().CheckAndApplyAsync();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "DispatcherUnhandledException");
        MessageBox.Show(
            "Ocorreu um erro inesperado. Os detalhes foram gravados no log.",
            "Auto Paint Heartopia",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        (_services as IDisposable)?.Dispose();
        Log.Information("Encerrando");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
