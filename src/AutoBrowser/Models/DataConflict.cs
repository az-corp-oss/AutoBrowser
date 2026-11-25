using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoBrowser.Models;

public enum DataConflictResolution
{
    KeepTarget,
    KeepSource,
    Merge
}

public partial class DataConflict : ObservableObject
{
    [ObservableProperty]
    private string _selectedOption;

    public DataConflict(string fileName, string targetName, string sourceName)
    {
        FileName = fileName;
        IsRulesFile = string.Equals(fileName, "rules.json", StringComparison.OrdinalIgnoreCase);
        KeepTargetLabel = $"Keep {targetName}";
        KeepSourceLabel = $"Keep {sourceName}";
        Options = IsRulesFile
            ? [KeepTargetLabel, KeepSourceLabel, "Merge rules"]
            : [KeepTargetLabel, KeepSourceLabel];
        _selectedOption = KeepTargetLabel;
    }

    public string FileName { get; }
    public bool IsRulesFile { get; }
    public string KeepTargetLabel { get; }
    public string KeepSourceLabel { get; }
    public IReadOnlyList<string> Options { get; }

    public DataConflictResolution Resolution =>
        SelectedOption == KeepSourceLabel ? DataConflictResolution.KeepSource :
        SelectedOption == "Merge rules" ? DataConflictResolution.Merge :
        DataConflictResolution.KeepTarget;
}
