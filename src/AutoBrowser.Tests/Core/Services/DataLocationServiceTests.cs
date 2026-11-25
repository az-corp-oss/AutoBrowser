using System.IO;
using AutoBrowser.Services;

namespace AutoBrowser.Tests.Services;

public class DataLocationServiceTests
{
    [Fact]
    public void SetUseGlobalData_BacksUpExistingDestinationFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "AutoBrowserDataLocation", Guid.NewGuid().ToString("N"));
        var portable = Path.Combine(root, "portable");
        var global = Path.Combine(root, "global");
        Directory.CreateDirectory(portable);
        Directory.CreateDirectory(global);
        File.WriteAllText(Path.Combine(portable, "rules.json"), "stale portable copy");
        File.WriteAllText(Path.Combine(global, "rules.json"), "newer global copy");

        try
        {
            var sut = new DataLocationService(portable, global, Path.Combine(root, "storage-mode.txt"));

            sut.SetUseGlobalData(true);

            Assert.True(sut.UseGlobalData);
            Assert.Equal("stale portable copy", File.ReadAllText(Path.Combine(global, "rules.json")));
            var backup = Directory.GetFiles(global, "rules.json.bak-*").Single();
            Assert.Equal("newer global copy", File.ReadAllText(backup));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
