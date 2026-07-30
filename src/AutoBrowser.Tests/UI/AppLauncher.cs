using System.Diagnostics;
using System.IO;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace AutoBrowser.Tests.UI;

public class AppLauncher : IDisposable
{
    private Process? _process;
    private UIA3Automation? _automation;
    private FlaUI.Core.Application? _app;
    private string? _tempDir;
    private bool _launched;

    public FlaUI.Core.Application App => _app ?? throw new InvalidOperationException("App not launched");
    public UIA3Automation Automation => _automation ?? throw new InvalidOperationException("Automation not initialized");

    private void DisposePrevious()
    {
        _process?.Dispose();
        _process = null;
        _automation?.Dispose();
        _automation = null;
        _app = null;
    }

    public FlaUI.Core.Application Launch()
    {
        if (_launched)
        {
            try
            {
                if (_process?.HasExited == false)
                    return _app!;
            }
            catch (InvalidOperationException)
            {
                // Process handle released, need to relaunch
            }

            _launched = false;
        }

        DisposePrevious();
        KillAllInstances();
        // Wait for processes to exit instead of hardcoded sleep
        for (int i = 0; i < 10 && System.Diagnostics.Process.GetProcessesByName("AutoBrowser").Length > 0; i++)
        {
            Thread.Sleep(100);
        }

        _tempDir = Path.Combine(Path.GetTempPath(), $"AutoBrowserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        var sourceDir = AppContext.BaseDirectory;
        CopyDirectory(sourceDir, _tempDir);

        var exePath = Path.Combine(_tempDir, "AutoBrowser.exe");
        Console.WriteLine($"Launching test app at {exePath}");

        _process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--no-single-instance --no-update-check --no-re-register-prompt",
            UseShellExecute = false
        });

        if (_process == null)
            throw new InvalidOperationException("Failed to start process");

        _automation = new UIA3Automation();
        _app = new FlaUI.Core.Application(_process);
        _launched = true;

        return _app;
    }

    private static void KillAllInstances()
    {
        var processes = Process.GetProcessesByName("AutoBrowser");
        foreach (var proc in processes)
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to signal process kill: {ex.Message}");
            }
        }

        // Wait for all signaled processes to exit to ensure complete teardown
        foreach (var proc in processes)
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.WaitForExit(2000);
                }
                proc.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to wait for process exit: {ex.Message}");
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        var dirs = dir.GetDirectories();
        Directory.CreateDirectory(destDir);

        foreach (var file in dir.GetFiles())
            file.CopyTo(Path.Combine(destDir, file.Name), true);

        foreach (var subdirectory in dirs)
            CopyDirectory(subdirectory.FullName, Path.Combine(destDir, subdirectory.Name));
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
                            for (var wait = 0; wait < 10; wait++)
                            {
                                try { if (window.IsOffscreen) break; } catch { break; }
                                Thread.Sleep(100);
                            }
                        }
                    }
                }

                if (!dismissed) break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to dismiss dialogs: {ex.Message}");
                break;
            }
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to clean up process during dispose: {ex.Message}");
        }

        KillAllInstances();

        _automation?.Dispose();
        _process?.Dispose();

        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete temp dir: {ex.Message}");
            }
        }
    }
}