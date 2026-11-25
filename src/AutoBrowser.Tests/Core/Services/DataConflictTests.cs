using AutoBrowser.Models;

namespace AutoBrowser.Tests.Services;

public class DataConflictTests
{
    [Fact]
    public void RulesConflict_OffersMergeAndMapsSelection()
    {
        var conflict = new DataConflict("rules.json", "global", "portable");

        Assert.Contains("Merge rules", conflict.Options);
        conflict.SelectedOption = "Keep portable";
        Assert.Equal(DataConflictResolution.KeepSource, conflict.Resolution);
        conflict.SelectedOption = "Merge rules";
        Assert.Equal(DataConflictResolution.Merge, conflict.Resolution);
    }

    [Fact]
    public void NonRulesConflict_DoesNotOfferMerge()
    {
        var conflict = new DataConflict("settings.json", "global", "portable");

        Assert.DoesNotContain("Merge rules", conflict.Options);
        Assert.Equal(DataConflictResolution.KeepTarget, conflict.Resolution);
    }
}
