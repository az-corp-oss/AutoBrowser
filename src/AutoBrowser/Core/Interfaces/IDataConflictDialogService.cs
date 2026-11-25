using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IDataConflictDialogService
{
    IReadOnlyList<DataConflict>? Show(IReadOnlyList<DataConflict> conflicts, string targetName, string sourceName);
}
