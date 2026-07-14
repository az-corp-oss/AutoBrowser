using AutoBrowser.Services;
using AutoBrowser.Helpers;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser;

public partial class App
{
    public void ProcessUrl(string url)
    {
        Log.Debug("ProcessUrl called: {Url}", url);

        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var settings = _settingsService.LoadSettings();
        var fallbackPath = settings.FallbackBrowserPath;
        var result = interceptor.TryRoute(url, fallbackPath);
        if (result.Type == RouteResultType.Forwarded)
        {
            Log.Information("URL routed via {Browser}: {Url}", result.BrowserDisplayName, url);
            if (settings.ShowPushNotifications)
            {
                var msg = string.IsNullOrEmpty(result.RuleName) ? $"Routed via {result.BrowserDisplayName}:\n{url}" : $"Routed via {result.BrowserDisplayName} ({result.RuleName}):\n{url}";
                ShowNotification("AutoBrowser", msg);
            }
            return;
        }
        else if (result.Type == RouteResultType.Dropped)
        {
            Log.Information("URL dropped by matching rule: {Url}", url);
            if (settings.ShowPushNotifications)
            {
                var msg = string.IsNullOrEmpty(result.RuleName) ? $"URL dropped by matching rule:\n{url}" : $"URL dropped by matching rule ({result.RuleName}):\n{url}";
                ShowNotification("AutoBrowser", msg);
            }
            return;
        }

        Log.Warning("No rule matched for URL: {Url}", url);
        if (settings.ShowPushNotifications)
        {
            ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
        }
    }

    public void ActivateFromTray(string? url = null)
    {
        if (_mainWindow == null) return;
        Log.Information("ActivateFromTray called, Url={Url}", url ?? "(none)");

        WindowForegroundHelper.BringToFront(_mainWindow);

        if (!string.IsNullOrEmpty(url))
        {
            Log.Information("Processing forwarded URL: {Url}", url);
            ProcessUrl(url);
        }

        Log.Debug("ActivateFromTray complete");
    }

    private static bool IsUrl(string? value) =>
        value is not null
        && (value.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    private bool TryRouteUrl(string url)
    {
        Log.Information("Routing URL via UrlInterceptorService");
        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var settings = _settingsService.LoadSettings();
        var fallbackPath = settings.FallbackBrowserPath;
        var result = interceptor.TryRoute(url, fallbackPath);

        if (result.Type == RouteResultType.Forwarded)
        {
            Log.Debug("URL routed via {Browser}, shutting down", result.BrowserDisplayName);
            if (settings.ShowPushNotifications)
            {
                var msg = string.IsNullOrEmpty(result.RuleName) ? $"Routed via {result.BrowserDisplayName}:\n{url}" : $"Routed via {result.BrowserDisplayName} ({result.RuleName}):\n{url}";
                ShowNotification("AutoBrowser", msg);
            }
            return true;
        }
        else if (result.Type == RouteResultType.Dropped)
        {
            Log.Debug("URL dropped, shutting down");
            if (settings.ShowPushNotifications)
            {
                var msg = string.IsNullOrEmpty(result.RuleName) ? $"URL dropped by matching rule:\n{url}" : $"URL dropped by matching rule ({result.RuleName}):\n{url}";
                ShowNotification("AutoBrowser", msg);
            }
            return true;
        }

        Log.Debug("No match for URL, showing notification and continuing to main window");
        if (settings.ShowPushNotifications)
        {
            ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
        }
        return false;
    }

    private void StartPipeServer()
    {
        _singleInstanceService = new SingleInstanceService();
        _singleInstanceService.StartServer(
            url =>
            {
                Log.Information("Second instance requested activation, Url={Url}", url ?? "(none)");
                ActivateFromTray(url);
            },
            Dispatcher);
    }
}
