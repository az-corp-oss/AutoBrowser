using System.IO;
using System.Text.RegularExpressions;

namespace AutoBrowser.Models;

public class RoutingRule
{
    public string Name { get; set; } = string.Empty;
    public string UrlPattern { get; set; } = string.Empty;
    public string BrowserPath { get; set; } = string.Empty;
    public string BrowserArguments { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Sequence { get; set; }

    public string BrowserDisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(BrowserPath))
                return string.Empty;

            var fileName = Path.GetFileNameWithoutExtension(BrowserPath);
            var known = BrowserDefinition.GetKnownBrowsers();
            var match = known.FirstOrDefault(b =>
                b.ExecutablePath.Equals(BrowserPath, StringComparison.OrdinalIgnoreCase));
            return match?.DisplayName ?? fileName;
        }
    }

    public bool IsMatch(string url)
    {
        if (string.IsNullOrWhiteSpace(UrlPattern) || string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            return Regex.IsMatch(url, UrlPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (RegexParseException)
        {
            return url.Contains(UrlPattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}
