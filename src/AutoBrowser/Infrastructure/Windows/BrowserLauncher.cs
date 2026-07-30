using System.Diagnostics;
using System.IO;
using Serilog;

namespace AutoBrowser.Services;

public class EdgeBrowserLauncher : IBrowserLauncher
{
    public bool CanLaunch(string browserPath) =>
        Path.GetFileNameWithoutExtension(browserPath).Equals("msedge", StringComparison.OrdinalIgnoreCase);

    public void Launch(string browserPath, string argumentsTemplate, string url)
    {
        Log.Verbose("Edge detected, using microsoft-edge protocol for tab reuse");
        Process.Start(new ProcessStartInfo
        {
            FileName = $"microsoft-edge:{url}",
            UseShellExecute = true
        });
    }
}

public class FirefoxBrowserLauncher : IBrowserLauncher
{
    public bool CanLaunch(string browserPath) =>
        Path.GetFileNameWithoutExtension(browserPath).Equals("firefox", StringComparison.OrdinalIgnoreCase);

    public void Launch(string browserPath, string argumentsTemplate, string url)
    {
        var args = argumentsTemplate.Replace("{url}", url);
        if (!args.Contains("-osint", StringComparison.OrdinalIgnoreCase))
            args = $"-osint -url \"{url}\"";

        LaunchInternal(browserPath, args);
    }

    private static void LaunchInternal(string browserPath, string args)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = browserPath,
            Arguments = args,
            UseShellExecute = false
        });
    }
}

public class GenericBrowserLauncher : IBrowserLauncher
{
    public bool CanLaunch(string browserPath) => true;

    public void Launch(string browserPath, string argumentsTemplate, string url)
    {
        var args = argumentsTemplate.Replace("{url}", url);
        Process.Start(new ProcessStartInfo
        {
            FileName = browserPath,
            Arguments = args,
            UseShellExecute = false
        });
    }
}