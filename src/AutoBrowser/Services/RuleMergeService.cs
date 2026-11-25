using System.Text.Json;
using AutoBrowser.Helpers;
using AutoBrowser.Models;

namespace AutoBrowser.Services;

public static class RuleMergeService
{
    public static string Merge(string targetJson, string sourceJson)
    {
        var targetGroups = Parse(targetJson);
        var sourceGroups = Parse(sourceJson);

        foreach (var sourceGroup in sourceGroups)
        {
            var targetGroup = targetGroups.FirstOrDefault(g =>
                string.Equals(g.Id, sourceGroup.Id, StringComparison.OrdinalIgnoreCase));

            if (targetGroup is null)
            {
                targetGroups.Add(sourceGroup);
                continue;
            }

            foreach (var rule in sourceGroup.Rules)
            {
                var duplicate = targetGroup.Rules.Any(existing => SameRule(existing, rule));
                if (!duplicate)
                {
                    targetGroup.Rules.Add(rule);
                }
            }
        }

        foreach (var group in targetGroups)
        {
            for (var i = 0; i < group.Rules.Count; i++)
            {
                group.Rules[i].Sequence = i + 1;
            }
        }

        return JsonSerializer.Serialize(new RoutingConfig { Groups = targetGroups },
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static List<RuleGroup> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            var config = JsonSerializer.Deserialize<RoutingConfig>(json);
            if (config?.Groups is { Count: > 0 }) return config.Groups;
        }
        catch (JsonException)
        {
        }

        try
        {
            var rules = JsonSerializer.Deserialize<List<RoutingRule>>(json);
            if (rules is not { Count: > 0 }) return [];

            return
            [
                new RuleGroup
                {
                    Id = UlidHelper.NewUlid(),
                    Name = "Migrated Rules",
                    IsEnabled = true,
                    Sequence = 1,
                    Rules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>(rules)
                }
            ];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool SameRule(RoutingRule left, RoutingRule right) =>
        string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.UrlPattern, right.UrlPattern, StringComparison.Ordinal) &&
        string.Equals(left.BrowserPath, right.BrowserPath, StringComparison.OrdinalIgnoreCase) &&
        left.Action == right.Action &&
        left.IsEnabled == right.IsEnabled;
}
