using System.Diagnostics;
using System.IO;
using AutoBrowser.Models;
using AutoBrowser.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Wpf.Ui.Appearance;

namespace AutoBrowser.ViewModels;

public partial class SettingsViewModel(
    IProtocolService protocolService,
    IDefaultBrowserService defaultBrowserService,
    ISettingsService settingsService,
    IBrowserProvider browserProvider,
    IWindowsSearchInterceptorService windowsSearchInterceptorService,
    IDataLocationService dataLocationService,
    IDataConflictDialogService dataConflictDialogService) : ObservableObject
{
    private readonly IProtocolService _protocolService = protocolService ?? throw new ArgumentNullException(nameof(protocolService));
    private readonly IDefaultBrowserService _defaultBrowserService = defaultBrowserService ?? throw new ArgumentNullException(nameof(defaultBrowserService));
    private readonly ISettingsService _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    private readonly IBrowserProvider _browserProvider = browserProvider ?? throw new ArgumentNullException(nameof(browserProvider));
    private readonly IWindowsSearchInterceptorService _windowsSearchInterceptorService = windowsSearchInterceptorService ?? throw new ArgumentNullException(nameof(windowsSearchInterceptorService));
    private readonly IDataLocationService _dataLocationService = dataLocationService ?? throw new ArgumentNullException(nameof(dataLocationService));
    private readonly IDataConflictDialogService _dataConflictDialogService = dataConflictDialogService ?? throw new ArgumentNullException(nameof(dataConflictDialogService));
    private bool _isInitialized;
    private bool _isChangingInterceptWindowsSearch;
    private bool _isChangingDataLocation;
    private DateTime _lastDataLocationChange = DateTime.MinValue;

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
    private bool _interceptWindowsSearch;

    [ObservableProperty]
    private bool _useGlobalData;

    public string DataLocation => _dataLocationService.DataDir;

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
        InterceptWindowsSearch = settings.InterceptWindowsSearch;
        UseGlobalData = _dataLocationService.UseGlobalData;
        OnPropertyChanged(nameof(DataLocation));

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

    partial void OnUseGlobalDataChanged(bool value)
    {
        if (!_isInitialized || _isChangingDataLocation) return;
        if (_dataLocationService.UseGlobalData == value) return;
        if (DateTime.UtcNow - _lastDataLocationChange < TimeSpan.FromMilliseconds(800)) return;

        _lastDataLocationChange = DateTime.UtcNow;
        _isChangingDataLocation = true;
        try
        {
            var conflicts = _dataLocationService.GetConflicts(value);
            IReadOnlyList<DataConflict>? resolutions = null;
            if (conflicts.Count > 0)
            {
                var targetName = value ? "global" : "portable";
                var sourceName = value ? "portable" : "global";
                resolutions = _dataConflictDialogService.Show(conflicts, targetName, sourceName);
                if (resolutions is null)
                {
                    UseGlobalData = !value;
                    Status = "Data location change cancelled";
                    return;
                }
            }

            _dataLocationService.SetUseGlobalData(value, resolutions);
            OnPropertyChanged(nameof(DataLocation));
            Status = value ? "Data stored in ~/.config/autobrowser" : "Data stored beside the app";
        }
        finally
        {
            _isChangingDataLocation = false;
        }
    }

    [RelayCommand]
    private void OpenDataLocation()
    {
        var path = _dataLocationService.DataDir;
        Directory.CreateDirectory(path);
        Log.Information("Opening data folder: {Path}", path);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = true
        });
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

    partial void OnInterceptWindowsSearchChanged(bool value)
    {
        if (!_isInitialized || _isChangingInterceptWindowsSearch) return;

        _isChangingInterceptWindowsSearch = true;
        try
        {
            Log.Debug("InterceptWindowsSearch changed to {Value}", value);

            if (value)
            {
                if (_windowsSearchInterceptorService.EnableInterception())
                {
                    Status = "Windows Search interception enabled";
                }
                else
                {
                    InterceptWindowsSearch = false;
                    Status = "Failed to enable interception";
                }
            }
            else
            {
                if (_windowsSearchInterceptorService.DisableInterception())
                {
                    Status = "Windows Search interception disabled";
                }
                else
                {
                    InterceptWindowsSearch = true;
                    Status = "Failed to disable interception";
                }
            }
        }
        finally
        {
            _isChangingInterceptWindowsSearch = false;
        }
    }
}