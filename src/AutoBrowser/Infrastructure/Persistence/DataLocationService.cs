using System.IO;
using Serilog;

namespace AutoBrowser.Services;

public class DataLocationService : IDataLocationService
{
    private static readonly string[] DataFiles = ["rules.json", "settings.json", "default_browser.txt"];

    public bool UseGlobalData => ReadUseGlobalData();

    public string DataDir => UseGlobalData ? Constants.GlobalDataDir : Constants.PortableDataDir;

    public void SetUseGlobalData(bool useGlobal)
    {
        if (UseGlobalData == useGlobal) return;

        var source = DataDir;
        var target = useGlobal ? Constants.GlobalDataDir : Constants.PortableDataDir;
        Directory.CreateDirectory(target);

        foreach (var file in DataFiles)
        {
            var sourcePath = Path.Combine(source, file);
            if (!File.Exists(sourcePath)) continue;

            File.Copy(sourcePath, Path.Combine(target, file), overwrite: true);
            Log.Information("Moved {File} to {Directory}", file, target);
        }

        File.WriteAllText(Constants.StorageModeFile, useGlobal ? "global" : "portable");
        Log.Information("Data storage mode set to {Mode}", useGlobal ? "global" : "portable");
    }

    private static bool ReadUseGlobalData()
    {
        if (!File.Exists(Constants.StorageModeFile)) return false;

        return string.Equals(File.ReadAllText(Constants.StorageModeFile).Trim(), "global",
            StringComparison.OrdinalIgnoreCase);
    }
}
