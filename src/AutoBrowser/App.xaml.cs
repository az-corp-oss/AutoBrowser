using AutoBrowser.Models;
using AutoBrowser.Services;
using Wpf.Ui.Appearance;

namespace AutoBrowser;

public partial class App : System.Windows.Application
{
    private static readonly string MutexName = "AutoBrowser-SingleInstance";
    private ISettingsService? _settingsService;
    public AppThemeMode CurrentThemeMode { get; private set; }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();

        if (e.Args.Length > 0)
        {
            var url = e.Args[0];
            if (url.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var interceptor = new UrlInterceptorService(
                    new RuleService(), new DefaultBrowserService());
                if (interceptor.TryRoute(url))
                {
                    Shutdown();
                    return;
                }
            }
        }

        using var mutex = new System.Threading.Mutex(true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            System.Windows.MessageBox.Show("AutoBrowser is already running in the system tray.",
                "AutoBrowser", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var settings = _settingsService.LoadSettings();
        ApplyTheme(settings.ThemeMode);

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    public void ApplyTheme(AppThemeMode mode)
    {
        var theme = mode == AppThemeMode.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;

        ApplicationThemeManager.Apply(theme);
        CurrentThemeMode = mode;
        _settingsService?.SaveSettings(new AppSettings { ThemeMode = mode });
    }
}
