using System.IO;
using AutoBrowser.Models;
using AutoBrowser.Services;
using Serilog;
using Wpf.Ui.Appearance;

namespace AutoBrowser;

public partial class App : System.Windows.Application
{
    private const string MutexName = "AutoBrowser-SingleInstance";

    private readonly ISettingsService _settingsService = new SettingsService();
    private SingleInstanceService? _singleInstanceService;
    private MainWindow? _mainWindow;
    private System.Threading.Mutex? _mutex;
    public AppThemeMode CurrentThemeMode { get; private set; }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        ConfigureLogging();
        RegisterExceptionHandlers();

        Log.Information("=== AutoBrowser Started ===");
        Log.Debug("OS: {OS}", Environment.OSVersion);
        Log.Debug("CLR: {CLR}", Environment.Version);
        Log.Debug("64-bit: {Is64Bit}", Environment.Is64BitProcess);
        Log.Debug("Path: {Path}", Environment.ProcessPath);
        Log.Debug("Args: {Args}", string.Join(" ", e.Args));

        base.OnStartup(e);

        // Extract URL arg once — used for early routing and later pipe signaling
        var urlArg = e.Args.Length > 0 ? e.Args[0] : null;
        if (IsUrl(urlArg))
        {
            Log.Debug("URL argument received: {Url}", urlArg);
            if (TryRouteUrl(urlArg!))
            {
                Shutdown();
                return;
            }
        }

        // Single-instance guard: if already running, signal it and exit
        _mutex = new System.Threading.Mutex(true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            Log.Information("Another instance detected, signaling via pipe");
            SingleInstanceService.SignalExistingInstance(urlArg);
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }
        Log.Information("Single instance mutex acquired");

        ApplyTheme(_settingsService.LoadSettings().ThemeMode);
        ShowMainWindow();
        StartPipeServer();
    }

    public void ApplyTheme(AppThemeMode mode)
    {
        Log.Debug("Applying theme: {Theme}", mode);
        var theme = mode == AppThemeMode.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(theme);
        CurrentThemeMode = mode;

        var settings = _settingsService.LoadSettings();
        settings.ThemeMode = mode;
        _settingsService.SaveSettings(settings);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Log.Information("OnExit (exit code: {ExitCode})", e.ApplicationExitCode);
        _singleInstanceService?.Dispose();
        _mutex?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    // ── Private helpers ──────────────────────────────────────────────

    private void ConfigureLogging()
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(logDir, "AutoBrowser-.log"),
                rollingInterval: RollingInterval.Day,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 14)
            .CreateLogger();
    }

    private void RegisterExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Log.Error(ex, "AppDomain.UnhandledException");
            Log.CloseAndFlush();
        };

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "DispatcherUnhandledException");
            args.Handled = true;
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "UnobservedTaskException");
            args.SetObserved();
        };
    }

    private static bool IsUrl(string? value) =>
        value is not null
        && (value.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Attempts to route a URL via rules without opening the UI.
    /// Returns <c>true</c> if the URL was matched and routed.
    /// </summary>
    private bool TryRouteUrl(string url)
    {
        Log.Information("Routing URL via UrlInterceptorService");
        var interceptor = new UrlInterceptorService(new RuleService(), new DefaultBrowserService());
        var fallbackPath = _settingsService.LoadSettings().FallbackBrowserPath;
        var browser = interceptor.TryRoute(url, fallbackPath);

        if (browser is not null)
        {
            Log.Debug("URL routed via {Browser}, shutting down", browser);
            ShowNotification("AutoBrowser", $"Routed via {browser}:\n{url}");
            return true;
        }

        Log.Debug("No match for URL, showing notification and continuing to main window");
        ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
        return false;
    }

    private void ShowMainWindow()
    {
        Log.Information("Creating MainWindow");
        _mainWindow = new MainWindow();
        _mainWindow.Show();
        Log.Information("MainWindow shown");
    }

    private void StartPipeServer()
    {
        _singleInstanceService = new SingleInstanceService();
        _singleInstanceService.StartServer(
            url =>
            {
                Log.Information("Second instance requested activation, Url={Url}", url ?? "(none)");
                _mainWindow?.ActivateFromTray(url);
            },
            Dispatcher);
    }

    private static void ShowNotification(string title, string message)
    {
        try
        {
            using var icon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""),
                Visible = true
            };
            icon.ShowBalloonTip(3000, title, message, System.Windows.Forms.ToolTipIcon.Warning);
        }
        catch { }
    }
}
