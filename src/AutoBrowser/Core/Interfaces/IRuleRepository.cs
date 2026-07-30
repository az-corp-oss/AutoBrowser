using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IRuleRepository
{
    List<RuleGroup> LoadGroups();
    void SaveGroups(List<RuleGroup> groups);
    Task<List<RuleGroup>> LoadGroupsAsync(CancellationToken ct = default);
    Task SaveGroupsAsync(List<RuleGroup> groups, CancellationToken ct = default);
}