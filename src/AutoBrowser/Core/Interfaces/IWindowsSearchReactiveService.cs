namespace AutoBrowser.Services;

public interface IWindowsSearchReactiveService : IDisposable
{
    void Start();
    void Stop();
    bool IsRunning { get; }
}