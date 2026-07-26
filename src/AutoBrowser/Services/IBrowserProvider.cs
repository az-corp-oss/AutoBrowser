using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IBrowserProvider
{
    IReadOnlyList<BrowserDefinition> GetInstalledBrowsers();
}