using AutoBrowser.Models;
using AutoBrowser.Helpers;

namespace AutoBrowser.Services;

public interface IDefaultRulesFactory
{
    List<RuleGroup> CreateDefaultGroups();
}

public class DefaultRulesFactory(IBrowserProvider browserProvider) : IDefaultRulesFactory
{
    private readonly IBrowserProvider _browserProvider = browserProvider ?? throw new ArgumentNullException(nameof(browserProvider));

    public List<RuleGroup> CreateDefaultGroups()
    {
        var browsers = _browserProvider.GetInstalledBrowsers();
        var edge = browsers.FirstOrDefault(b => b.Name.Contains("edge", StringComparison.OrdinalIgnoreCase));
        var chrome = browsers.FirstOrDefault(b => b.Name.Contains("chrome", StringComparison.OrdinalIgnoreCase));

        var groups = new List<RuleGroup>();

        var workRules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>();
        if (edge != null)
        {
            workRules.Add(new RoutingRule
            {
                Name = "Work sites",
                UrlPattern = @"(teams|office|sharepoint|outlook|microsoft)\.com",
                BrowserPath = edge.ExecutablePath,
                BrowserArguments = edge.ArgumentsTemplate,
                Sequence = 1
            });
        }
        // Only add group if it has rules
        if (workRules.Count > 0)
        {
            groups.Add(new RuleGroup
            {
                Id = UlidHelper.NewUlid(),
                Name = "Work",
                IsEnabled = true,
                Sequence = 1,
                Rules = workRules
            });
        }

        var socialRules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>();
        if (chrome != null)
        {
            socialRules.Add(new RoutingRule
            {
                Name = "Social & Entertainment",
                UrlPattern = @"(youtube|reddit|twitter|x\.com|instagram|facebook)\.com",
                BrowserPath = chrome.ExecutablePath,
                BrowserArguments = chrome.ArgumentsTemplate,
                Sequence = 1
            });
        }
        // Only add group if it has rules
        if (socialRules.Count > 0)
        {
            groups.Add(new RuleGroup
            {
                Id = UlidHelper.NewUlid(),
                Name = "Social",
                IsEnabled = true,
                Sequence = 2,
                Rules = socialRules
            });
        }

        var devBrowserPath = chrome?.ExecutablePath ?? edge?.ExecutablePath ?? "";
        var devBrowserArgs = chrome?.ArgumentsTemplate ?? edge?.ArgumentsTemplate ?? "{url}";
        var devRules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>();
        if (!string.IsNullOrEmpty(devBrowserPath))
        {
            devRules.Add(new RoutingRule
            {
                Name = "Development",
                UrlPattern = @"(github|gitlab|stackoverflow|npmjs|docker)\.(com|io)",
                BrowserPath = devBrowserPath,
                BrowserArguments = devBrowserArgs,
                Sequence = 1
            });
        }
        groups.Add(new RuleGroup
        {
            Id = UlidHelper.NewUlid(),
            Name = "Development",
            IsEnabled = true,
            Sequence = 3,
            Rules = devRules
        });

        return groups;
    }
}