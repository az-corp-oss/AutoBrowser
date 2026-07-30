using System.Diagnostics;
using System.IO;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Services;

public class UrlInterceptorService(
    IRuleService ruleService,
    IDefaultBrowserService defaultBrowserService,
    IBrowserProvider browserProvider,
    IEnumerable<IBrowserLauncher> launchers)
{
    private readonly IRuleService _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
    private readonly IDefaultBrowserService _defaultBrowserService = defaultBrowserService ?? throw new ArgumentNullException(nameof(defaultBrowserService));
    private readonly IBrowserProvider _browserProvider = browserProvider ?? throw new ArgumentNullException(nameof(browserProvider));
    private readonly IEnumerable<IBrowserLauncher> _launchers = launchers ?? throw new ArgumentNullException(nameof(launchers));

    public RouteResult TryRoute(string url, string? fallbackBrowserPath = null)
    {
        Log.Information("TryRoute called with URL: {Url}", url);

        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Verbose("URL is null or whitespace, returning NoMatch");
            Log.Information("TryRoute completed: NoMatch (null/whitespace URL)");
            return new RouteResult(RouteResultType.NoMatch, null);
        }

        url = url.Trim();
        url = StripProtocolPrefix(url);

        var rules = _ruleService.LoadRules()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Sequence)
            .ToList();

        Log.Debug("Loaded {Count} enabled rules", rules.Count);

        foreach (var rule in rules)
        {
            Log.Verbose("Checking rule '{RuleName}' (Sequence: {Sequence}, Pattern: {Pattern})", rule.Name, rule.Sequence, rule.UrlPattern);
            if (!rule.IsMatch(url))
            {
                Log.Verbose("Rule '{RuleName}' does not match", rule.Name);
                continue;
            }

            if (rule.IsForward)
            {
                Log.Verbose("Rule '{RuleName}' matched, launching browser: {BrowserPath}", rule.Name, rule.BrowserPath);
                LaunchBrowser(rule.BrowserPath, rule.BrowserArguments, url);
                Log.Information("TryRoute completed: {Browser} (matched rule: {RuleName})", rule.BrowserDisplayName, rule.Name);
                return new RouteResult(RouteResultType.Forwarded, rule.BrowserDisplayName, rule.Name);
            }
            else
            {
                Log.Information("Rule '{RuleName}' matched with Drop action. URL dropped: {Url}", rule.Name, url);
                return new RouteResult(RouteResultType.Dropped, null, rule.Name);
            }
        }

        Log.Verbose("No rules matched, checking fallback browser");
        if (!string.IsNullOrEmpty(fallbackBrowserPath) && File.Exists(fallbackBrowserPath))
        {
            Log.Verbose("Using selected fallback browser: {BrowserPath}", fallbackBrowserPath);
            LaunchBrowser(fallbackBrowserPath, "{url}", url);
            var displayName = ResolveBrowserDisplayName(fallbackBrowserPath);
            Log.Information("TryRoute completed: {Browser} (fallback)", displayName);
            return new RouteResult(RouteResultType.Forwarded, displayName);
        }

        Log.Information("No rules matched and no fallback browser set for URL: {Url}", url);
        Log.Information("TryRoute completed: NoMatch (no rules matched)");
        return new RouteResult(RouteResultType.NoMatch, null);
    }

    private static string StripProtocolPrefix(string url)
    {
        Log.Debug("StripProtocolPrefix called with URL: {Url}", url);
        var prefixes = new[] { "autobrowser:", "autobrowser://" };
        foreach (var prefix in prefixes)
        {
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(prefix.Length).TrimStart('/');
                Log.Verbose("Removed prefix '{Prefix}', result: {Url}", prefix, url);
                break;
            }
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
            Log.Verbose("Added https:// prefix, result: {Url}", url);
        }

        Log.Debug("StripProtocolPrefix completed: {Url}", url);
        return url;
    }

    private void LaunchBrowser(string browserPath, string argumentsTemplate, string url)
    {
        Log.Information("LaunchBrowser called - Path: {BrowserPath}, ArgsTemplate: {ArgsTemplate}, URL: {Url}", browserPath, argumentsTemplate, url);

        var launcher = _launchers.FirstOrDefault(l => l.CanLaunch(browserPath))
            ?? _launchers.FirstOrDefault(l => l is GenericBrowserLauncher);

        if (launcher != null)
        {
            Log.Verbose("Found launcher: {LauncherType}", launcher.GetType().Name);
            launcher.Launch(browserPath, argumentsTemplate, url);
        }
        else
        {
            Log.Warning("No valid browser launcher found for {BrowserPath}", browserPath);
        }
    }



    private string ResolveBrowserDisplayName(string browserPath)
    {
        var known = _browserProvider.GetInstalledBrowsers();
        var match = known.FirstOrDefault(b =>
            b.ExecutablePath.Equals(browserPath, StringComparison.OrdinalIgnoreCase));
        return match?.DisplayName ?? Path.GetFileNameWithoutExtension(browserPath);
    }

    private static void OpenInDefaultBrowser(string url)
    {
        Log.Information("OpenInDefaultBrowser called with URL: {Url}", url);
        Log.Verbose("Opening URL with system default browser");
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
        Log.Information("OpenInDefaultBrowser completed successfully");
    }
}
