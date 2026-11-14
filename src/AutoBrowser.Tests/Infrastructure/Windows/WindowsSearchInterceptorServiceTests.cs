using AutoBrowser.Models;
using AutoBrowser.Services;
using Microsoft.Win32;
using Moq;
using Serilog;

namespace AutoBrowser.Tests.Infrastructure.Windows;

public class WindowsSearchInterceptorServiceTests : IDisposable
{
    private const string TestBaseKey = @"Software\AutoBrowser.Tests";
    private const string ProgIdKey = @"Software\Classes\AutoBrowser.microsoft-edge";
    private const string ProgIdCommandKey = @"Software\Classes\AutoBrowser.microsoft-edge\shell\open\command";
    private const string EdgeProgIdKey = @"Software\Classes\microsoft-edge";
    private const string EdgeCommandKey = @"Software\Classes\microsoft-edge\shell\open\command";

    private readonly string _instanceKeyName;
    private readonly RegistryKey _rootKey;
    private readonly Mock<IProtocolService> _protocolService = new();
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly Mock<IWindowsSearchReactiveService> _reactiveService = new();
    private readonly WindowsSearchInterceptorService _sut;

    public WindowsSearchInterceptorServiceTests()
    {
        _instanceKeyName = $@"{TestBaseKey}\{Guid.NewGuid():N}";
        _rootKey = Registry.CurrentUser.CreateSubKey(_instanceKeyName)
            ?? throw new InvalidOperationException("Failed to create test registry key.");

        _settingsService.Setup(x => x.LoadSettings()).Returns(() => new AppSettings());
        _sut = new TestableInterceptorService(
            _rootKey,
            _protocolService.Object,
            _settingsService.Object,
            _reactiveService.Object);
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(TestBaseKey, throwOnMissingSubKey: false);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Best-effort cleanup of {Key} failed", TestBaseKey);
        }
    }

    private sealed class TestableInterceptorService : WindowsSearchInterceptorService
    {
        private readonly RegistryKey _rootKey;

        public TestableInterceptorService(
            RegistryKey rootKey,
            IProtocolService protocolService,
            ISettingsService settingsService,
            IWindowsSearchReactiveService reactiveService)
            : base(protocolService, settingsService, reactiveService)
        {
            _rootKey = rootKey;
        }

        protected override RegistryKey RootKey => _rootKey;
    }

    [Fact]
    public void EnableInterception_RegistersProgIds_AndPersistsSetting()
    {
        var result = _sut.EnableInterception();

        Assert.True(result);

        using var progId = _rootKey.OpenSubKey(ProgIdKey);
        Assert.NotNull(progId);
        Assert.Equal("URL:AutoBrowser Microsoft Edge Redirect", progId!.GetValue(""));
        Assert.Equal(string.Empty, progId.GetValue("URL Protocol"));

        using var progIdCmd = _rootKey.OpenSubKey(ProgIdCommandKey);
        Assert.NotNull(progIdCmd);
        Assert.Matches("^\"[^\"]+\" \"%1\"$", progIdCmd!.GetValue("")?.ToString() ?? string.Empty);

        using var edgeCmd = _rootKey.OpenSubKey(EdgeCommandKey);
        Assert.NotNull(edgeCmd);

        _reactiveService.Verify(x => x.Start(), Times.Once);
        _settingsService.Verify(x => x.SaveSettings(It.Is<AppSettings>(s => s.InterceptWindowsSearch)), Times.Once);
    }

    [Fact]
    public void DisableInterception_WhenWeCreatedKeys_RemovesEdgeRegistration()
    {
        _sut.EnableInterception();

        var result = _sut.DisableInterception();

        Assert.True(result);
        Assert.Null(_rootKey.OpenSubKey(ProgIdKey));
        Assert.Null(_rootKey.OpenSubKey(EdgeProgIdKey));

        _reactiveService.Verify(x => x.Stop(), Times.Once);
        _settingsService.Verify(x => x.SaveSettings(It.Is<AppSettings>(s => !s.InterceptWindowsSearch)), Times.Once);
    }

    [Fact]
    public void DisableInterception_WhenOriginalCommandExisted_RestoresIt()
    {
        using (var originalCmd = _rootKey.CreateSubKey(EdgeCommandKey))
        {
            originalCmd.SetValue("", "\"C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe\" \"%1\"");
        }

        _sut.EnableInterception();
        using (var taken = _rootKey.OpenSubKey(EdgeCommandKey))
        {
            Assert.Matches("^\"[^\"]+\" \"%1\"$", taken!.GetValue("")?.ToString() ?? string.Empty);
        }

        _sut.DisableInterception();
        using var restored = _rootKey.OpenSubKey(EdgeCommandKey);
        Assert.NotNull(restored);
        Assert.Equal("\"C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe\" \"%1\"", restored!.GetValue(""));
    }

    [Fact]
    public void EnableInterception_CalledTwice_PreservesOriginalCommandBackup()
    {
        using (var originalCmd = _rootKey.CreateSubKey(EdgeCommandKey))
        {
            originalCmd.SetValue("", "\"C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe\" \"%1\"");
        }

        _sut.EnableInterception();
        _sut.EnableInterception();
        _sut.DisableInterception();

        using var restored = _rootKey.OpenSubKey(EdgeCommandKey);
        Assert.NotNull(restored);
        Assert.Equal("\"C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe\" \"%1\"", restored!.GetValue(""));
    }

    [Fact]
    public void DisableInterception_WhenOriginalCommandWasMissing_DeletesCommandSubKey()
    {
        _rootKey.CreateSubKey(EdgeProgIdKey).Dispose();

        _sut.EnableInterception();
        var result = _sut.DisableInterception();

        Assert.True(result);
        using var edge = _rootKey.OpenSubKey(EdgeProgIdKey);
        Assert.NotNull(edge);
        Assert.Null(edge!.OpenSubKey("shell\\open\\command"));
    }

    [Fact]
    public void DisableInterception_WhenEdgeRegistrationWasHijacked_LeavesItUntouched()
    {
        _sut.EnableInterception();
        using (var hijacked = _rootKey.CreateSubKey(EdgeCommandKey))
        {
            hijacked.SetValue("", "\"C:\\OtherApp\\edge.exe\" \"%1\"");
        }

        _sut.DisableInterception();
        Assert.Null(_rootKey.OpenSubKey(ProgIdKey));
        using var edgeCmd = _rootKey.OpenSubKey(EdgeCommandKey);
        Assert.NotNull(edgeCmd);
        Assert.Equal("\"C:\\OtherApp\\edge.exe\" \"%1\"", edgeCmd!.GetValue(""));
    }
}