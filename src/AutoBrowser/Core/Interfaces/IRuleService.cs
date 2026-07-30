using AutoBrowser.Models;
using System.Threading;

namespace AutoBrowser.Services;

public interface IRuleService
{
    List<RuleGroup> LoadGroups();
    void SaveGroups(List<RuleGroup> groups);
    List<RoutingRule> LoadRules();
    Task<List<RuleGroup>> LoadGroupsAsync(CancellationToken ct = default);
    Task SaveGroupsAsync(List<RuleGroup> groups, CancellationToken ct = default);
    Task<List<RoutingRule>> LoadRulesAsync(CancellationToken ct = default);
}
