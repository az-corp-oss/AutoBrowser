using AutoBrowser.Models;
using AutoBrowser.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Wpf.Ui.Appearance;

namespace AutoBrowser.ViewModels;

public partial class SettingsViewModel(
    IProtocolService protocolService,
    IDefaultBrowserService defaultBrowserService,
    ISettingsService settingsService,
    IBrowserProvider browserProvider) : ObservableObject
{
    private readonly IProtocolService _protocolService = protocolService ?? throw new ArgumentNullException(nameof(protocolService));
    private readonly IDefaultBrowserService _defaultBrowserService = defaultBrowserService ?? throw new ArgumentNullException(nameof(defaultBrowserService));
    private readonly ISettingsService _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    private readonly IBrowserProvider _browserProvider = browserProvider ?? throw new ArgumentNullException(nameof(browserProvider));
    private bool _isInitialized;

    public IReadOnlyList<BrowserDefinition> AvailableBrowsers => _browserProvider.GetInstalledBrowsers();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDarkTheme))]
    private ApplicationTheme _currentApplicationTheme = ApplicationTheme.Unknown;

    [ObservableProperty]
    private BrowserDefinition? _fallbackBrowser;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _closeToTray;
    [ObservableProperty]
    private bool _showPushNotifications = true;

    [ObservableProperty]
    private string _status = "Ready";

    public bool IsDarkTheme
    {
        get => CurrentApplicationTheme == ApplicationTheme.Dark;
        set => CurrentApplicationTheme = value ? ApplicationTheme.Dark : ApplicationTheme.Light;
    }

    public bool IsProtocolRegistered
    {
        get => _protocolService.IsProtocolRegistered();
        set
        {
            if (value)
            {
                _protocolService.RegisterProtocolHandler();
                Status = "autobrowser:// protocol registered";
            }
            else
            {
                _protocolService.UnregisterProtocolHandler();
                Status = "autobrowser:// protocol unregistered";
            }
            OnPropertyChanged();
        }
    }

    public bool IsDefaultBrowser
    {
        get => _defaultBrowserService.IsDefaultBrowser();
        set
        {
            if (value)
            {
                _defaultBrowserService.RegisterAsDefaultBrowser();
                Status = "Registered as default browser — select AutoBrowser in Settings > Default Apps";
            }
            else
            {
                _defaultBrowserService.UnregisterAsDefaultBrowser();
                Status = "Unregistered as default browser";
            }
            OnPropertyChanged();
        }
    }

    public void Initialize()
    {
        if (_isInitialized) return;

        var settings = _settingsService.LoadSettings();
        if (!string.IsNullOrEmpty(settings.FallbackBrowserPath))
            FallbackBrowser = AvailableBrowsers.FirstOrDefault(b =>
                b.ExecutablePath.Equals(settings.FallbackBrowserPath, StringComparison.OrdinalIgnoreCase));

        MinimizeToTray = settings.MinimizeToTray;
        CloseToTray = settings.CloseToTray;
        ShowPushNotifications = settings.ShowPushNotifications;

        CurrentApplicationTheme = ApplicationThemeManager.GetAppTheme();
        ApplicationThemeManager.Changed += OnThemeChanged;
        _isInitialized = true;
    }

    partial void OnCurrentApplicationThemeChanged(ApplicationTheme oldValue, ApplicationTheme newValue)
    {
        if (oldValue == newValue) return;
        ApplicationThemeManager.Apply(newValue);

        var app = Application.Current as App;
        app?.SaveTheme(newValue == ApplicationTheme.Dark ? AppThemeMode.Dark : AppThemeMode.Light);

        Status = newValue == ApplicationTheme.Dark ? "Switched to Dark theme" : "Switched to Light theme";
    }

    private void OnThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
    {
        if (CurrentApplicationTheme != currentApplicationTheme)
        {
            CurrentApplicationTheme = currentApplicationTheme;
        }
    }

    partial void OnFallbackBrowserChanged(BrowserDefinition? value)
    {
        var settings = _settingsService.LoadSettings();
        settings.FallbackBrowserPath = value?.ExecutablePath ?? string.Empty;
        _settingsService.SaveSettings(settings);
        Status = value is not null ? $"Fallback: {value.DisplayName}" : "Fallback browser cleared";
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        Log.Debug("MinimizeToTray changed to {Value}", value);
        var settings = _settingsService.LoadSettings();
        settings.MinimizeToTray = value;
        _settingsService.SaveSettings(settings);
        Status = value ? "Minimize to tray enabled" : "Minimize to tray disabled";
    }

    partial void OnCloseToTrayChanged(bool value)
    {
        Log.Debug("CloseToTray changed to {Value}", value);
        var settings = _settingsService.LoadSettings();
        settings.CloseToTray = value;
        _settingsService.SaveSettings(settings);
        Status = value ? "Close to tray enabled" : "Close to tray disabled";
    }

    partial void OnShowPushNotificationsChanged(bool value)
    {
        Log.Debug("ShowPushNotifications changed to {Value}", value);
        var settings = _settingsService.LoadSettings();
        settings.ShowPushNotifications = value;
        _settingsService.SaveSettings(settings);
        Status = value ? "Push notifications enabled" : "Push notifications disabled";
    }
}