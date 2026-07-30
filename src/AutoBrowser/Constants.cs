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

    public static readonly string DataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
}