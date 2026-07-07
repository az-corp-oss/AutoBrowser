using System.Diagnostics;
using System.IO;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Services;

public class UrlInterceptorService
{
    private readonly IRuleService _ruleService;
    private readonly IDefaultBrowserService _defaultBrowserService;

    public UrlInterceptorService(IRuleService ruleService, IDefaultBrowserService defaultBrowserService)
    {
        _ruleService = ruleService;
        _defaultBrowserService = defaultBrowserService;
    }

    public string? TryRoute(string url, string? fallbackBrowserPath = null)
    {
        Log.Information("TryRoute called with URL: {Url}", url);

        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Verbose("URL is null or whitespace, returning null");
            Log.Information("TryRoute completed: null (null/whitespace URL)");
            return null;
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

            Log.Verbose("Rule '{RuleName}' matched, launching browser: {BrowserPath}", rule.Name, rule.BrowserPath);
            LaunchBrowser(rule.BrowserPath, rule.BrowserArguments, url);
            Log.Information("TryRoute completed: {Browser} (matched rule: {RuleName})", rule.BrowserDisplayName, rule.Name);
            return rule.BrowserDisplayName;
        }

        Log.Verbose("No rules matched, checking fallback browser");
        if (!string.IsNullOrEmpty(fallbackBrowserPath) && File.Exists(fallbackBrowserPath))
        {
            Log.Verbose("Using selected fallback browser: {BrowserPath}", fallbackBrowserPath);
            LaunchBrowser(fallbackBrowserPath, "{url}", url);
            var displayName = ResolveBrowserDisplayName(fallbackBrowserPath);
            Log.Information("TryRoute completed: {Browser} (fallback)", displayName);
            return displayName;
        }

        Log.Information("No rules matched and no fallback browser set for URL: {Url}", url);
        Log.Information("TryRoute completed: null (no rules matched)");
        return null;
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

    private static void LaunchBrowser(string browserPath, string argumentsTemplate, string url)
    {
        Log.Information("LaunchBrowser called - Path: {BrowserPath}, ArgsTemplate: {ArgsTemplate}, URL: {Url}", browserPath, argumentsTemplate, url);
        var args = argumentsTemplate.Replace("{url}", url);
        Log.Verbose("Initial args after URL replacement: {Args}", args);

        if (IsFirefox(browserPath) && !args.Contains("-osint", StringComparison.OrdinalIgnoreCase))
        {
            args = $"-osint -url \"{url}\"";
        }

        if (IsEdge(browserPath))
        {
            Log.Verbose("Edge detected, using microsoft-edge protocol for tab reuse");
            Process.Start(new ProcessStartInfo
            {
                FileName = $"microsoft-edge:{url}",
                UseShellExecute = true
            });
        }
        else
        {
            Log.Verbose("Starting process: {BrowserPath} {Args}", browserPath, args);
            Process.Start(new ProcessStartInfo
            {
                FileName = browserPath,
                Arguments = args,
                UseShellExecute = false
            });
        }
        Log.Information("LaunchBrowser completed successfully");
    }

    private static bool IsEdge(string browserPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(browserPath);
        return fileName.Equals("msedge", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFirefox(string browserPath)
    {
        Log.Debug("IsFirefox called with path: {BrowserPath}", browserPath);
        var fileName = Path.GetFileNameWithoutExtension(browserPath);
        var isFirefox = fileName.Equals("firefox", StringComparison.OrdinalIgnoreCase);
        Log.Verbose("IsFirefox result: {IsFirefox} (FileName: {FileName})", isFirefox, fileName);
        return isFirefox;
    }

    private static string ResolveBrowserDisplayName(string browserPath)
    {
        var known = BrowserDefinition.GetKnownBrowsers();
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
