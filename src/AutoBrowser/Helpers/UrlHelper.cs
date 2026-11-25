namespace AutoBrowser.Helpers;

public static class UrlHelper
{
    public static bool IsUrl(string? value) =>
        value is not null &&
        (value.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("microsoft-edge:", StringComparison.OrdinalIgnoreCase));
}
