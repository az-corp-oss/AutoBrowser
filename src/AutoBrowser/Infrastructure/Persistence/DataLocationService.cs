using System.IO;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Services;

public class DataLocationService : IDataLocationService
{
    private static readonly string[] DataFiles = ["rules.json", "settings.json", "default_browser.txt"];

    private readonly string _portableDir;
    private readonly string _globalDir;
    private readonly string _modeFile;

    public DataLocationService()
        : this(Constants.PortableDataDir, Constants.GlobalDataDir, Constants.StorageModeFile)
    {
    }

    public DataLocationService(string portableDir, string globalDir, string modeFile)
    {
        _portableDir = portableDir;
        _globalDir = globalDir;
        _modeFile = modeFile;
    }

    public bool UseGlobalData => ReadUseGlobalData();

    public string DataDir => UseGlobalData ? _globalDir : _portableDir;

    public IReadOnlyList<DataConflict> GetConflicts(bool useGlobal)
    {
        var source = useGlobal ? _portableDir : _globalDir;
        var target = useGlobal ? _globalDir : _portableDir;
        var targetName = useGlobal ? "global" : "portable";
        var sourceName = useGlobal ? "portable" : "global";

        var conflicts = new List<DataConflict>();
        foreach (var file in DataFiles)
        {
            var sourcePath = Path.Combine(source, file);
            var targetPath = Path.Combine(target, file);
            if (!File.Exists(sourcePath) || !File.Exists(targetPath)) continue;
            if (string.Equals(File.ReadAllText(sourcePath), File.ReadAllText(targetPath), StringComparison.Ordinal)) continue;

            conflicts.Add(new DataConflict(file, targetName, sourceName));
        }

        return conflicts;
    }

    public void SetUseGlobalData(bool useGlobal, IReadOnlyList<DataConflict>? resolutions = null)
    {
        if (UseGlobalData == useGlobal) return;

        var source = DataDir;
        var target = useGlobal ? _globalDir : _portableDir;
        Directory.CreateDirectory(target);

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        foreach (var file in DataFiles)
        {
            var sourcePath = Path.Combine(source, file);
            if (!File.Exists(sourcePath)) continue;

            var targetPath = Path.Combine(target, file);
            var resolution = resolutions?.FirstOrDefault(c =>
                string.Equals(c.FileName, file, StringComparison.OrdinalIgnoreCase))?.Resolution
                ?? DataConflictResolution.KeepSource;

            if (resolution == DataConflictResolution.KeepTarget) continue;

            if (File.Exists(targetPath))
            {
                var backupPath = BackupExistingFile(targetPath, stamp);
                Log.Warning("Backed up existing {File} to {Backup}", targetPath, backupPath);
            }

            if (resolution == DataConflictResolution.Merge && File.Exists(targetPath))
            {
                var merged = RuleMergeService.Merge(File.ReadAllText(targetPath), File.ReadAllText(sourcePath));
                File.WriteAllText(targetPath, merged);
                Log.Information("Merged {File} into {Directory}", file, target);
                continue;
            }

            File.Copy(sourcePath, targetPath, overwrite: true);
            Log.Information("Moved {File} to {Directory}", file, target);
        }

        File.WriteAllText(_modeFile, useGlobal ? "global" : "portable");
        Log.Information("Data storage mode set to {Mode}", useGlobal ? "global" : "portable");
    }

    private static string BackupExistingFile(string path, string stamp)
    {
        var backup = $"{path}.bak-{stamp}";
        var suffix = 1;
        while (File.Exists(backup))
        {
            backup = $"{path}.bak-{stamp}-{suffix++}";
        }

        File.Copy(path, backup);
        return backup;
    }

    private bool ReadUseGlobalData()
    {
        if (!File.Exists(_modeFile)) return false;

        return string.Equals(File.ReadAllText(_modeFile).Trim(), "global",
            StringComparison.OrdinalIgnoreCase);
    }
}
