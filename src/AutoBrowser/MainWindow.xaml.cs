using System.Windows;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoBrowser;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private bool _isExiting;

    public MainWindow()
    {
        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public void ProcessUrl(string url)
    {
        var interceptor = new UrlInterceptorService(
            new RuleService(), new DefaultBrowserService());
        var fallbackPath = _viewModel.FallbackBrowser?.ExecutablePath;
        var browser = interceptor.TryRoute(url, fallbackPath);
        if (browser is not null)
        {
            if (IsLoaded)
                _viewModel.Status = $"Routed via {browser}: {url}";
            return;
        }

        _viewModel.Status = $"No match: {url}";
        ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
    }

    private static void ShowNotification(string title, string message)
    {
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
        catch { }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetupTrayIcon();

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var url = args[1];
            if (url.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                ProcessUrl(url);
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
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
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
        _viewModel.SaveRules();

        if (!_isExiting)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
        else
        {
            _trayIcon?.Dispose();
        }
    }

    private void ExitApp()
    {
        _isExiting = true;
        _trayIcon?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
