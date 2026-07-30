using Microsoft.Win32;
using Serilog;

namespace AutoBrowser.Services;

public class ProtocolService : IProtocolService
{
    private const string AppName = Constants.AppName;
    private const string ProtocolName = Constants.ProtocolName;

    public bool RegisterProtocolHandler()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return false;

            using var key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{ProtocolName}");
            key.SetValue("", $"URL:{AppName} Protocol");
            key.SetValue("URL Protocol", "");

            using var commandKey = key.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", $"\"{exePath}\" \"%1\"");

            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "RegisterProtocolHandler failed");
            return false;
        }
    }

    public bool UnregisterProtocolHandler()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProtocolName}", false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "UnregisterProtocolHandler failed");
            return false;
        }
    }

    public bool IsProtocolRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                $@"Software\Classes\{ProtocolName}");
            return key != null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "IsProtocolRegistered failed");
            return false;
        }
    }

    public string? GetRegisteredPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProtocolName}\shell\open\command");
            var cmd = key?.GetValue("") as string;
            if (string.IsNullOrEmpty(cmd)) return null;

            cmd = cmd.Trim();
            if (cmd.StartsWith('"'))
            {
                var end = cmd.IndexOf('"', 1);
                return end > 0 ? cmd[1..end] : null;
            }
            var space = cmd.IndexOf(' ');
            return space > 0 ? cmd[..space] : cmd;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "GetRegisteredPath failed");
            return null;
        }
    }
}
