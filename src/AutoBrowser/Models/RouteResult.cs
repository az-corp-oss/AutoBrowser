namespace AutoBrowser.Models;

public enum RouteResultType
{
    NoMatch,
    Forwarded,
    Dropped
}

public record RouteResult(RouteResultType Type, string? BrowserDisplayName, string? RuleName = null);
