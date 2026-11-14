using Microsoft.Win32;
using Serilog;

namespace AutoBrowser.Services;

public class WindowsSearchInterceptorService : IWindowsSearchInterceptorService
{
    private const string ProgIdKey = @"Software\Classes\AutoBrowser.microsoft-edge";
    private const string ProgIdCommandKey = @"Software\Classes\AutoBrowser.microsoft-edge\shell\open\command";
    private const string EdgeProgIdKey = @"Software\Classes\microsoft-edge";
    private const string EdgeCommandKey = @"Software\Classes\microsoft-edge\shell\open\command";
    private const string OriginalCommandValue = "OriginalCommand";
    private readonly IProtocolService _protocolService;
    private readonly ISettingsService _settingsService;
    private readonly IWindowsSearchReactiveService _reactiveService;

    protected virtual RegistryKey RootKey => Registry.CurrentUser;

    public WindowsSearchInterceptorService(
        IProtocolService protocolService,
        ISettingsService settingsService,
        IWindowsSearchReactiveService reactiveService)
    {
        _protocolService = protocolService;
        _settingsService = settingsService;
        _reactiveService = reactiveService;
    }

    public bool IsInterceptionEnabled()
    {
        var settings = _settingsService.LoadSettings();
        return settings.InterceptWindowsSearch;
    }

    public bool EnableInterception()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return false;

            RegisterProgId(exePath);

            _reactiveService.Start();

            var settings = _settingsService.LoadSettings();
            settings.InterceptWindowsSearch = true;
            _settingsService.SaveSettings(settings);

            Log.Information("Windows Search interception enabled (ProgId + Reactive watcher)");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to enable Windows Search interception");
            return false;
        }
    }

    private string? GetApplicationOpenCommand()
    {
        var exePath = Environment.ProcessPath;
        return string.IsNullOrEmpty(exePath) ? null : $"\"{exePath}\" \"%1\"";
    }

    private void RegisterProgId(string exePath)
    {
        try
        {
            var command = GetApplicationOpenCommand();
            if (command == null)
                throw new InvalidOperationException("Process path is not available.");

            string? originalCommand = null;
            var edgeCommandIsOurs = false;
            using (var existingEdgeKey = RootKey.OpenSubKey(EdgeProgIdKey))
            {
                if (existingEdgeKey != null)
                {
                    using var existingCmdKey = existingEdgeKey.OpenSubKey("shell\\open\\command");
                    var currentCommand = existingCmdKey?.GetValue("") as string;
                    if (currentCommand == null)
                    {
                        originalCommand = "";
                    }
                    else if (string.Equals(currentCommand, command, StringComparison.OrdinalIgnoreCase))
                    {
                        edgeCommandIsOurs = true;
                    }
                    else
                    {
                        originalCommand = currentCommand;
                    }
                }
            }

            using (var key = RootKey.CreateSubKey(ProgIdKey))
            {
                key.SetValue("", "URL:AutoBrowser Microsoft Edge Redirect");
                key.SetValue("URL Protocol", "");
                if (originalCommand != null)
                {
                    key.SetValue(OriginalCommandValue, originalCommand);
                }
                else if (edgeCommandIsOurs && key.GetValue(OriginalCommandValue) == null)
                {
                    key.SetValue(OriginalCommandValue, string.Empty);
                }
            }

            using (var cmdKey = RootKey.CreateSubKey(ProgIdCommandKey))
            {
                cmdKey.SetValue("", command);
            }

            using (var edgeKey = RootKey.CreateSubKey(EdgeProgIdKey))
            {
                edgeKey.SetValue("", "URL:microsoft-edge");
                edgeKey.SetValue("URL Protocol", "");
            }

            using (var edgeCmdKey = RootKey.CreateSubKey(EdgeCommandKey))
            {
                edgeCmdKey.SetValue("", command);
            }

            Log.Information("Registered AutoBrowser.microsoft-edge ProgId (HKCU)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register AutoBrowser.microsoft-edge ProgId");
            throw;
        }
    }

    private void UnregisterProgId()
    {
        try
        {
            string? originalCommand = null;
            using (var progIdKey = RootKey.OpenSubKey(ProgIdKey))
            {
                originalCommand = progIdKey?.GetValue(OriginalCommandValue) as string;
            }

            RootKey.DeleteSubKeyTree(ProgIdKey, false);


            var command = GetApplicationOpenCommand();
            if (command == null) return;
            using (var edgeKey = RootKey.OpenSubKey(EdgeProgIdKey, writable: true))
            {
                if (edgeKey == null) return;

                string? currentCommand;
                using (var currentCmdKey = edgeKey.OpenSubKey("shell\\open\\command"))
                {
                    currentCommand = currentCmdKey?.GetValue("") as string;
                }

                if (!string.Equals(currentCommand, command, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("HKCU microsoft-edge registration is not managed by AutoBrowser, leaving it untouched");
                    return;
                }

                if (originalCommand != null)
                {
                    if (originalCommand.Length == 0)
                    {
                        edgeKey.DeleteSubKey("shell\\open\\command", false);
                    }
                    else
                    {
                        using var cmdKey = edgeKey.CreateSubKey("shell\\open\\command");
                        cmdKey.SetValue("", originalCommand);
                    }
                }
                else
                {
                    RootKey.DeleteSubKeyTree(EdgeProgIdKey, false);
                }
            }

            Log.Information("Unregistered AutoBrowser.microsoft-edge ProgId");
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "ProgId unregister (may not exist)");
        }
    }

    public bool DisableInterception()
    {
        try
        {
            _reactiveService.Stop();
            UnregisterProgId();

            var settings = _settingsService.LoadSettings();
            settings.InterceptWindowsSearch = false;
            _settingsService.SaveSettings(settings);

            Log.Information("Windows Search interception disabled");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to disable Windows Search interception");
            return false;
        }
    }

    public void SyncWithSettings()
    {
        var settings = _settingsService.LoadSettings();
        if (settings.InterceptWindowsSearch)
        {
            EnableInterception();
        }
        else
        {
            DisableInterception();
        }
    }
}