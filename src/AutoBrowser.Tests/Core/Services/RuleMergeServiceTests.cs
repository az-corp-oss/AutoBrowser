using System.Text.Json;
using AutoBrowser.Models;
using AutoBrowser.Services;

namespace AutoBrowser.Tests.Services;

public class RuleMergeServiceTests
{
    [Fact]
    public void Merge_KeepsTargetRulesAndAppendsNewSourceRules()
    {
        var group = new RuleGroup
        {
            Id = "group-1",
            Name = "Work",
            IsEnabled = true,
            Sequence = 1
        };
        group.Rules.Add(new RoutingRule { Name = "Existing", UrlPattern = "example.com", Sequence = 1 });
        var target = JsonSerializer.Serialize(new RoutingConfig { Groups = [group] });

        var sourceGroup = new RuleGroup
        {
            Id = "group-1",
            Name = "Work",
            IsEnabled = true,
            Sequence = 1
        };
        sourceGroup.Rules.Add(new RoutingRule { Name = "Existing", UrlPattern = "example.com", Sequence = 1 });
        sourceGroup.Rules.Add(new RoutingRule { Name = "New", UrlPattern = "sample.org", Sequence = 2 });
        var source = JsonSerializer.Serialize(new RoutingConfig { Groups = [sourceGroup] });

        var merged = RuleMergeService.Merge(target, source);

        var config = JsonSerializer.Deserialize<RoutingConfig>(merged);
        Assert.NotNull(config);
        var rules = Assert.Single(config.Groups).Rules;
        Assert.Equal(2, rules.Count);
        Assert.Contains(rules, r => r.Name == "Existing");
        Assert.Contains(rules, r => r.Name == "New");
        Assert.Equal([1, 2], rules.Select(r => r.Sequence).OrderBy(s => s));
    }
}
