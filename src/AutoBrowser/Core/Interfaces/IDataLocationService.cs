namespace AutoBrowser.Services;

public interface IDataLocationService
{
    bool UseGlobalData { get; }
    string DataDir { get; }
    void SetUseGlobalData(bool useGlobal);
}
