namespace AutoBrowser.Services;

public interface IWindowsSearchInterceptorService
{
    bool IsInterceptionEnabled();
    bool EnableInterception();
    bool DisableInterception();
    void SyncWithSettings();
}