using AutoBrowser.Models;
using System.Threading;

namespace AutoBrowser.Services;

public class RuleService(IRuleRepository ruleRepository, IDefaultRulesFactory defaultRulesFactory) : IRuleService
{
    private readonly IRuleRepository _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
    private readonly IDefaultRulesFactory _defaultRulesFactory = defaultRulesFactory ?? throw new ArgumentNullException(nameof(defaultRulesFactory));

    public List<RuleGroup> LoadGroups()
    {
        var groups = _ruleRepository.LoadGroups();

        if (groups.Count > 0)
            return groups;

        groups = _defaultRulesFactory.CreateDefaultGroups();
        _ruleRepository.SaveGroups(groups);
        return groups;
    }

    public void SaveGroups(List<RuleGroup> groups) => _ruleRepository.SaveGroups(groups);

    public List<RoutingRule> LoadRules()
    {
        return LoadGroups()
            .Where(g => g.IsEnabled)
            .OrderBy(g => g.Sequence)
            .SelectMany(g => g.Rules.OrderBy(r => r.Sequence))
            .ToList();
    }

    public async Task<List<RuleGroup>> LoadGroupsAsync(CancellationToken ct = default)
    {
        var groups = await _ruleRepository.LoadGroupsAsync(ct).ConfigureAwait(false);

        if (groups.Count > 0)
        {
            return groups;
        }

        // Only create defaults on first run (empty file), not on every load
        groups = _defaultRulesFactory.CreateDefaultGroups();
        await _ruleRepository.SaveGroupsAsync(groups, ct).ConfigureAwait(false);
        return groups;
    }

    public async Task SaveGroupsAsync(List<RuleGroup> groups, CancellationToken ct = default)
    {
        await _ruleRepository.SaveGroupsAsync(groups, ct).ConfigureAwait(false);
    }

    public async Task<List<RoutingRule>> LoadRulesAsync(CancellationToken ct = default)
    {
        var groups = await LoadGroupsAsync(ct).ConfigureAwait(false);
        return groups
            .Where(g => g.IsEnabled)
            .OrderBy(g => g.Sequence)
            .SelectMany(g => g.Rules.OrderBy(r => r.Sequence))
            .ToList();
    }
}