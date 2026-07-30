namespace AutoBrowser.Services;

public interface IBrowserLauncher
{
    bool CanLaunch(string browserPath);
    void Launch(string browserPath, string argumentsTemplate, string url);
}