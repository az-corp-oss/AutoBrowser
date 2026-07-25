using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IRuleService
{
    List<RuleGroup> LoadGroups();
    void SaveGroups(List<RuleGroup> groups);
    List<RoutingRule> LoadRules();
}
