using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IDataLocationService
{
    bool UseGlobalData { get; }
    string DataDir { get; }
    IReadOnlyList<DataConflict> GetConflicts(bool useGlobal);
    void SetUseGlobalData(bool useGlobal, IReadOnlyList<DataConflict>? resolutions = null);
}
