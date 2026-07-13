using System.Diagnostics;
using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace AutoBrowser.Tests.UI;

public class AppLauncher : IDisposable
{
    private Process? _process;
    private UIA3Automation? _automation;
    private FlaUI.Core.Application? _app;
    private string? _tempDir;

    public FlaUI.Core.Application App => _app ?? throw new InvalidOperationException("App not launched");
    public UIA3Automation Automation => _automation ?? throw new InvalidOperationException("Automation not initialized");

    public FlaUI.Core.Application Launch()
    {
        // Kill any lingering instances that hold the mutex
        foreach (var proc in Process.GetProcessesByName("AutoBrowser"))
        {
            try { proc.Kill(); proc.WaitForExit(2000); } catch { }
        }
        Thread.Sleep(500);

        _tempDir = Path.Combine(Path.GetTempPath(), $"AutoBrowserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var sourceDir = AppContext.BaseDirectory;

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(_tempDir, Path.GetFileName(file)), true);
        }

        var sourceDataDir = Path.Combine(sourceDir, "Data");
        if (Directory.Exists(sourceDataDir))
        {
            var destDataDir = Path.Combine(_tempDir, "Data");
            Directory.CreateDirectory(destDataDir);
            foreach (var file in Directory.GetFiles(sourceDataDir))
            {
                File.Copy(file, Path.Combine(destDataDir, Path.GetFileName(file)), true);
            }
        }

        var exePath = Path.Combine(_tempDir, "AutoBrowser.exe");

        _process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--no-single-instance --no-update-check --no-re-register-prompt",
            UseShellExecute = false
        });

        _automation = new UIA3Automation();
        _app = FlaUI.Core.Application.Attach(_process);
        return _app;
    }

    public void DismissBlockingDialogs(int retries = 3)
    {
        if (_app == null) return;

        for (var attempt = 0; attempt < retries; attempt++)
        {
            try
            {
                var allWindows = _app.GetAllTopLevelWindows(_automation!);
                var dismissed = false;

                foreach (var window in allWindows)
                {
                    foreach (var label in new[] { "No", "Cancel", "Close" })
                    {
                        var button = window.FindFirstDescendant(cf =>
                            cf.ByControlType(ControlType.Button).And(cf.ByText(label)));
                        if (button != null)
                        {
                            button.Click();
                            dismissed = true;
                            Thread.Sleep(500);
                        }
                    }
                }

                if (!dismissed) break;
            }
            catch { break; }
        }
    }

    public void Dispose()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit(5000);
            }
        }
        catch { }

        _automation?.Dispose();
        _process?.Dispose();

        try
        {
            if (_tempDir != null && Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }
}
