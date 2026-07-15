using System.Windows;
using Serilog;

namespace AutoBrowser;

public partial class App
{
    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""),
            Text = "AutoBrowser - URL Router",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show Window", null, (_, _) => ShowWindow());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();
        _trayIcon.BalloonTipClicked += (_, _) => ShowWindow();

        Log.Debug("Tray icon created and visible");
    }

    private void ShowWindow()
    {
        if (_mainWindow == null) return;
        Log.Debug("ShowWindow called from tray");
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        Log.Debug("Window restored from tray");
    }

    private void ExitApp()
    {
        Log.Information("ExitApp called from tray context menu");
        SaveWindowState();
        _isExiting = true;
        _trayIcon?.Dispose();
        
        // Force the app to shutdown immediately without relying on MainWindow.Closing routing
        Current.Shutdown();
    }

    private void ShowNotification(string title, string message)
    {
        try
        {
            if (_trayIcon == null) return;
            _trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            Log.Debug("Balloon tip shown: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to show notification");
        }
    }
}
