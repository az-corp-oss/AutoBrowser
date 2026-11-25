using AutoBrowser.Models;
using AutoBrowser.Views;
using Serilog;

namespace AutoBrowser.Services;

public class DataConflictDialogService : IDataConflictDialogService
{
    public IReadOnlyList<DataConflict>? Show(IReadOnlyList<DataConflict> conflicts, string targetName, string sourceName)
    {
        try
        {
            Log.Information("Showing data conflict dialog for {Count} files", conflicts.Count);
            var view = new DataConflictView(conflicts, targetName, sourceName);
            return view.ShowDialog() == true ? view.Conflicts : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to show data conflict dialog");
            return null;
        }
    }
}
