using System.IO;

namespace AutoBrowser.Models;

public class BrowserDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string ArgumentsTemplate { get; set; } = "{url}";

    public static string? ParseExePath(string commandLine)
    {
        commandLine = commandLine.Trim();
        if (commandLine.StartsWith('"'))
        {
            var end = commandLine.IndexOf('"', 1);
            return end > 0 ? commandLine[1..end] : null;
        }

        var firstSpace = commandLine.IndexOf(' ');
        return firstSpace > 0 ? commandLine[..firstSpace] : commandLine;
    }

    public static string? ResolveDisplayName(string exeName)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["msedge"] = "Microsoft Edge",
            ["chrome"] = "Google Chrome",
            ["firefox"] = "Mozilla Firefox",
            ["opera"] = "Opera",
            ["brave"] = "Brave",
            ["vivaldi"] = "Vivaldi",
            ["msedge.exe"] = "Microsoft Edge",
            ["chrome.exe"] = "Google Chrome",
            ["firefox.exe"] = "Mozilla Firefox",
            ["opera.exe"] = "Opera",
            ["brave.exe"] = "Brave",
            ["vivaldi.exe"] = "Vivaldi",
        };

        return map.TryGetValue(exeName, out var name) ? name : null;
    }
}