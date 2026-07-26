using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IRuleService
{
    List<RuleGroup> LoadGroups();
    void SaveGroups(List<RuleGroup> groups);
    List<RoutingRule> LoadRules();
    Task<List<RuleGroup>> LoadGroupsAsync();
    Task SaveGroupsAsync(List<RuleGroup> groups);
    Task<List<RoutingRule>> LoadRulesAsync();
}
