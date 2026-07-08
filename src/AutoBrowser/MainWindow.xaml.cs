using System.Windows;
using AutoBrowser.Helpers;
using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Serilog;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoBrowser;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private bool _isExiting;

    public MainWindow()
    {
        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        _viewModel = new MainViewModel();
        _settingsService = new SettingsService();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closing += OnClosing;

        RestoreWindowState();

        Log.Debug("MainWindow initialized: MinimizeToTray={Minimize}, CloseToTray={Close}",
            _viewModel.MinimizeToTray, _viewModel.CloseToTray);
    }

    public void ProcessUrl(string url)
    {
        Log.Debug("ProcessUrl called: {Url}", url);

        var interceptor = new UrlInterceptorService(
            new RuleService(), new DefaultBrowserService());
        var fallbackPath = _viewModel.FallbackBrowser?.ExecutablePath;
        var browser = interceptor.TryRoute(url, fallbackPath);
        if (browser is not null)
        {
            Log.Information("URL routed via {Browser}: {Url}", browser, url);
            ShowNotification("AutoBrowser", $"Routed via {browser}:\n{url}");
            if (IsLoaded)
                _viewModel.Status = $"Routed via {browser}: {url}";
            return;
        }

        Log.Warning("No rule matched for URL: {Url}", url);
        _viewModel.Status = $"No match: {url}";
        ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
    }

    private static void ShowNotification(string title, string message)
    {
        Log.Debug("ShowNotification: {Title} - {Message}", title, message);
        try
        {
            using var icon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    Environment.ProcessPath ?? ""),
                Visible = true
            };
            icon.ShowBalloonTip(3000, title, message,
                System.Windows.Forms.ToolTipIcon.Warning);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ShowNotification failed");
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("MainWindow loaded, setting up tray icon");
        SetupTrayIcon();
        _viewModel.StartSilentUpdateCheck();
        CheckAndPromptReRegister();

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var url = args[1];
            Log.Debug("Command-line URL argument: {Url}", url);
            if (url.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                ProcessUrl(url);
            }
            else
            {
                Log.Debug("Ignoring non-URL argument: {Url}", url);
            }
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                Environment.ProcessPath ?? ""),
            Text = "AutoBrowser - URL Router",
            Visible = true
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();

        menu.Items.Add("Show Window", null, (_, _) => ShowWindow());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        Log.Debug("Tray icon created and visible");
    }

    private void ShowWindow()
    {
        Log.Debug("ShowWindow called from tray");
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Log.Debug("Window restored from tray");
    }

    /// <summary>
    /// Called by the single-instance pipe server when a second instance is launched.
    /// Brings this window to the foreground and optionally routes a URL.
    /// </summary>
    public void ActivateFromTray(string? url = null)
    {
        Log.Information("ActivateFromTray called, Url={Url}", url ?? "(none)");

        WindowForegroundHelper.BringToFront(this);

        if (!string.IsNullOrEmpty(url))
        {
            Log.Information("Processing forwarded URL: {Url}", url);
            ProcessUrl(url);
        }

        Log.Debug("ActivateFromTray complete");
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        Log.Debug("Window_StateChanged: WindowState={WindowState}, MinimizeToTray={MinimizeToTray}",
            WindowState, _viewModel.MinimizeToTray);
        if (WindowState == WindowState.Minimized && _viewModel.MinimizeToTray)
            Hide();
    }

    private void CheckBox_Toggled(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.SaveRules();
    }

    private void RuleListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _viewModel.EditRuleCommand.Execute(null);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowState();
        _viewModel.SaveRules();
        Log.Debug("OnClosing: _isExiting={IsExiting}, CloseToTray={CloseToTray}",
            _isExiting, _viewModel.CloseToTray);

        if (!_isExiting && _viewModel.CloseToTray)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            _isExiting = true;
            _trayIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void ExitApp()
    {
        Log.Information("ExitApp called from tray context menu");
        SaveWindowState();
        _isExiting = true;
        _trayIcon?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void RestoreWindowState()
    {
        var settings = _settingsService.LoadSettings();

        Width = settings.WindowWidth;
        Height = settings.WindowHeight;

        if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
        {
            Left = settings.WindowLeft;
            Top = settings.WindowTop;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        if (settings.IsMaximized)
            WindowState = WindowState.Maximized;

        Log.Debug("Window state restored: {Width}x{Height} at ({Left},{Top}), Maximized={Maximized}",
            Width, Height, Left, Top, settings.IsMaximized);
    }

    private void SaveWindowState()
    {
        var settings = _settingsService.LoadSettings();

        if (WindowState == WindowState.Maximized)
        {
            settings.IsMaximized = true;
            settings.WindowLeft = RestoreBounds.Left;
            settings.WindowTop = RestoreBounds.Top;
            settings.WindowWidth = RestoreBounds.Width;
            settings.WindowHeight = RestoreBounds.Height;
        }
        else
        {
            settings.IsMaximized = false;
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
        }

        _settingsService.SaveSettings(settings);
        Log.Debug("Window state saved: {Width}x{Height} at ({Left},{Top}), Maximized={Maximized}",
            settings.WindowWidth, settings.WindowHeight, settings.WindowLeft, settings.WindowTop, settings.IsMaximized);
    }

    private async void CheckAndPromptReRegister()
    {
        Log.Information("CheckAndPromptReRegister called");

        var currentPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            Log.Debug("Cannot determine current process path, skipping re-register check");
            return;
        }

        var protocolService = new ProtocolService();
        var defaultBrowserService = new DefaultBrowserService();

        var needsReRegister = false;
        var registrationType = string.Empty;

        // Check autobrowser:// protocol registration
        if (protocolService.IsProtocolRegistered())
        {
            var registeredPath = protocolService.GetRegisteredPath();
            Log.Debug("Protocol registration: RegisteredPath={Registered}, CurrentPath={Current}",
                registeredPath, currentPath);

            if (!string.IsNullOrEmpty(registeredPath)
                && !registeredPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
            {
                needsReRegister = true;
                registrationType = "autobrowser:// protocol handler";
            }
        }

        // Check default browser registration
        if (defaultBrowserService.IsDefaultBrowser())
        {
            var registeredPath = defaultBrowserService.GetRegisteredPath();
            Log.Debug("Default browser registration: RegisteredPath={Registered}, CurrentPath={Current}",
                registeredPath, currentPath);

            if (!string.IsNullOrEmpty(registeredPath)
                && !registeredPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
            {
                needsReRegister = true;
                registrationType = string.IsNullOrEmpty(registrationType)
                    ? "system default browser"
                    : registrationType + " and system default browser";
            }
        }

        if (needsReRegister)
        {
            Log.Information("App path has changed, prompting user to re-register: {Type}", registrationType);

            var oldProtocolPath = protocolService.IsProtocolRegistered() ? protocolService.GetRegisteredPath() : null;
            var oldDefaultPath = defaultBrowserService.IsDefaultBrowser() ? defaultBrowserService.GetRegisteredPath() : null;
            var oldPath = oldProtocolPath ?? oldDefaultPath ?? "(unknown)";

            var dialog = new Wpf.Ui.Controls.MessageBox
            {
                Title = "AutoBrowser — Path Changed",
                Content = $"AutoBrowser has been moved to a new location, but the {registrationType} still points to the old path.\n\n" +
                          $"Old path: {oldPath}\n" +
                          $"New path: {currentPath}\n\n" +
                          "Would you like to re-register now?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                IsCloseButtonEnabled = false,
                Width = 500,
                MinWidth = 500
            };
            dialog.Owner = this;
            var result = await dialog.ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                if (registrationType.Contains("protocol"))
                {
                    protocolService.UnregisterProtocolHandler();
                    protocolService.RegisterProtocolHandler();
                    Log.Information("Protocol handler re-registered");
                }
                if (registrationType.Contains("default browser"))
                {
                    defaultBrowserService.UnregisterAsDefaultBrowser();
                    defaultBrowserService.RegisterAsDefaultBrowser();
                    Log.Information("Default browser registration updated");
                }

                ShowNotification("AutoBrowser", "Registration updated successfully.");
                _viewModel.Status = "Registration updated to new path.";
            }
            else
            {
                Log.Debug("User declined re-registration");
            }
        }
        else
        {
            Log.Debug("Registration paths are current, no re-registration needed");
        }
    }
}
