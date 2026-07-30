using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AutoBrowser.ViewModels;
using Serilog;

namespace AutoBrowser;

public partial class App
{
    private void ShowMainWindow()
    {
        Log.Information("Creating MainWindow");
        _mainWindow = Services.GetRequiredService<MainWindow>();
        Log.Information("MainWindow constructed, wiring events");

        _mainWindow.Loaded += async (s, e) =>
        {
            try
            {
                await MainWindow_Loaded(s, e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during MainWindow_Loaded execution");
            }
        };
        _mainWindow.Closing += MainWindow_Closing;
        _mainWindow.StateChanged += MainWindow_StateChanged;
        Log.Information("Events wired, restoring state");

        RestoreWindowState();
        Log.Information("State restored, calling Show()");

        // Apply saved theme BEFORE showing the window to avoid Light→Dark flash
        var savedTheme = _settingsService.LoadSettings().ThemeMode;
        ApplyTheme(savedTheme);

        try
        {
            _mainWindow.Show();
            Log.Information("MainWindow shown, isVisible={IsVisible}", _mainWindow.IsVisible);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to show MainWindow");
        }
    }

    private async Task MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("MainWindow loaded, setting up tray icon");
        SetupTrayIcon();

        var vm = Services.GetRequiredService<MainViewModel>();

        if (_mainWindow == null) return;

        // Delay prompt and update check so window is fully rendered and content is completely visible first
        _mainWindow.ContentRendered += async (s, ev) =>
        {
            // Give extra frame time for full visual layout stability
            await Task.Delay(200);

            if (!_parsedArgs.SkipUpdate)
            {
                // Run silent update check first
                await vm.StartSilentUpdateCheckAsync(_parsedArgs.ForceUpdate);
            }

            if (!_parsedArgs.SkipReRegister)
            {
                // Then prompt path re-registration
                await CheckAndPromptReRegister();
            }

            if (_parsedArgs.Url != null)
            {
                Log.Debug("Command-line URL argument: {Url}", _parsedArgs.Url);
                ProcessUrl(_parsedArgs.Url);
            }
        };
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (_mainWindow == null) return;
        var settings = _settingsService.LoadSettings();
        Log.Debug("Window_StateChanged: WindowState={WindowState}, MinimizeToTray={MinimizeToTray}",
            _mainWindow.WindowState, settings.MinimizeToTray);
        if (_mainWindow.WindowState == WindowState.Minimized && settings.MinimizeToTray)
        {
            _mainWindow.Hide();
            _notificationService.Show("AutoBrowser", "Minimized to tray. Click here to restore the window.");
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_mainWindow == null) return;
        var settings = _settingsService.LoadSettings();
        SaveWindowState();
        Log.Debug("MainWindow_Closing: _isExiting={IsExiting}, CloseToTray={CloseToTray}",
            _isExiting, settings.CloseToTray);

        if (!_isExiting && settings.CloseToTray)
        {
            e.Cancel = true;
            _mainWindow.Hide();
            _notificationService.Show("AutoBrowser", "Closed to tray. Click here to restore the window.");
        }
        else
        {
            _isExiting = true;
            _trayIcon?.Dispose();
            // Force shutdown to prevent background threads/server keeping app alive
            Application.Current.Shutdown();
        }
    }

    private void RestoreWindowState()
    {
        if (_mainWindow == null) return;
        Log.Debug("RestoreWindowState: loading settings");
        var settings = _settingsService.LoadSettings();
        Log.Debug("RestoreWindowState: settings loaded, applying");

        _mainWindow.Width = settings.WindowWidth;
        _mainWindow.Height = settings.WindowHeight;

        if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
        {
            _mainWindow.Left = settings.WindowLeft;
            _mainWindow.Top = settings.WindowTop;
            _mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        }

        if (settings.IsMaximized)
            _mainWindow.WindowState = WindowState.Maximized;

        Log.Debug("Window state restored: {Width}x{Height} at ({Left},{Top}), Maximized={Maximized}",
            _mainWindow.Width, _mainWindow.Height, _mainWindow.Left, _mainWindow.Top, settings.IsMaximized);
    }

    private void SaveWindowState()
    {
        if (_mainWindow == null) return;
        var settings = _settingsService.LoadSettings();

        if (_mainWindow.WindowState == WindowState.Maximized)
        {
            settings.IsMaximized = true;
            settings.WindowLeft = _mainWindow.RestoreBounds.Left;
            settings.WindowTop = _mainWindow.RestoreBounds.Top;
            settings.WindowWidth = _mainWindow.RestoreBounds.Width;
            settings.WindowHeight = _mainWindow.RestoreBounds.Height;
        }
        else
        {
            settings.IsMaximized = false;
            settings.WindowLeft = _mainWindow.Left;
            settings.WindowTop = _mainWindow.Top;
            settings.WindowWidth = _mainWindow.Width;
            settings.WindowHeight = _mainWindow.Height;
        }

        _settingsService.SaveSettings(settings);
        Log.Debug("Window state saved: {Width}x{Height} at ({Left},{Top}), Maximized={Maximized}",
            settings.WindowWidth, settings.WindowHeight, settings.WindowLeft, settings.WindowTop, settings.IsMaximized);
    }
}
