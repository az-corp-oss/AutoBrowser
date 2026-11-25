using System;
using System.IO;

namespace AutoBrowser;

public static class Constants
{
    public const string AppName = "AutoBrowser";
    public const string ProtocolName = "autobrowser";
    public const string MutexName = "AutoBrowser-SingleInstance";
    public const string PipeName = "AutoBrowser-SingleInstancePipe";
    public const string ProgId = "AutoBrowserLink";

    public static readonly string PortableDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

    public static readonly string GlobalDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autobrowser");

    public static readonly string StorageModeFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage-mode.txt");
}
