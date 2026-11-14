using System.Diagnostics;
using System.Management;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Services;

public class WindowsSearchReactiveService : IWindowsSearchReactiveService
{
    private const string AppName = "AutoBrowser";

    private readonly App _app;

    private System.Threading.Timer? _pollTimer;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);
    private readonly HashSet<int> _seenEdgePids = new();
    private readonly object _lock = new();
    private bool _isRunning;
    private bool _disposed;

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
    }

    public WindowsSearchReactiveService(App app)
    {
        _app = app;
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning) return;

            Log.Information("Starting Windows Search reactive interception");
            _isRunning = true;
            _pollTimer = new System.Threading.Timer(PollForEdgeLaunches, null, TimeSpan.Zero, _pollInterval);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            Log.Information("Stopping Windows Search reactive interception");
            _isRunning = false;
            _pollTimer?.Dispose();
            _pollTimer = null;
            _seenEdgePids.Clear();
        }
    }

    private void PollForEdgeLaunches(object? state)
    {
        lock (_lock)
        {
            if (_disposed || !_isRunning) return;
        }

        try
        {
            HashSet<int> currentEdgePids;
            lock (_lock)
            {
                currentEdgePids = Process.GetProcessesByName("msedge")
                    .Select(p =>
                    {
                        var id = p.Id;
                        p.Dispose();
                        return id;
                    })
                    .ToHashSet();

                _seenEdgePids.RemoveWhere(pid => !currentEdgePids.Contains(pid));
            }

            foreach (var pid in currentEdgePids)
            {
                CheckAndHandleEdgeProcess(pid);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "PollForEdgeLaunches error");
        }
    }

    private void CheckAndHandleEdgeProcess(int pid)
    {
        lock (_lock)
        {
            if (_disposed || !_isRunning) return;

            if (!_seenEdgePids.Add(pid)) return;
        }

        Process? edgeProc = null;
        try
        {
            edgeProc = Process.GetProcessById(pid);
            if (edgeProc.ProcessName.Equals("msedge", StringComparison.OrdinalIgnoreCase))
            {
                var cmdLine = GetCommandLine(edgeProc);
                if (!string.IsNullOrEmpty(cmdLine) && IsMicrosoftEdgeUri(cmdLine))
                {
                    Log.Information("Detected Windows Search Edge launch: PID={Pid}, CmdLine={CmdLine}", pid, cmdLine);

                    var url = ExtractUrlFromEdgeCommandLine(cmdLine);
                    if (!string.IsNullOrEmpty(url))
                    {
                        _app.Dispatcher.InvokeAsync(() =>
                        {
                            Log.Information("Routing intercepted Windows Search URL: {Url}", url);
                            var result = _app.ProcessUrl(url);
                            if (result.Type == RouteResultType.Forwarded || result.Type == RouteResultType.Dropped)
                            {
                                KillEdgeProcess(pid);
                            }
                        });
                    }
                }
            }
        }
        catch (ArgumentException ex)
        {
            Log.Debug(ex, "Process already exited");
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "CheckAndHandleEdgeProcess error for PID {Pid}", pid);
        }
        finally
        {
            edgeProc?.Dispose();
        }
    }

    private static void KillEdgeProcess(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            proc.Kill(true);
        }
        catch (ArgumentException ex)
        {
            Log.Debug(ex, "Process already exited");
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to kill Edge process PID {Pid}", pid);
        }
    }

    private static string? GetCommandLine(Process process)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["CommandLine"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to read command line via WMI for PID {Pid}", process.Id);
        }
        return null;
    }

    private static bool IsMicrosoftEdgeUri(string commandLine)
    {
        return commandLine.Contains("microsoft-edge:", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractUrlFromEdgeCommandLine(string commandLine)
    {
        var idx = commandLine.IndexOf("microsoft-edge:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var fullUri = commandLine.Substring(idx).Trim().Trim('"', '\'');
        var spaceIdx = fullUri.IndexOf(' ');
        if (spaceIdx > 0)
        {
            fullUri = fullUri.Substring(0, spaceIdx);
        }

        return fullUri;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _isRunning = false;
            _pollTimer?.Dispose();
            _pollTimer = null;
            _seenEdgePids.Clear();
        }
        GC.SuppressFinalize(this);
    }
}