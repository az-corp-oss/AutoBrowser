namespace AutoBrowser.Models;

public class AppSettings
{
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Light;
    public string LastTestUrl { get; set; } = "https://";
}
