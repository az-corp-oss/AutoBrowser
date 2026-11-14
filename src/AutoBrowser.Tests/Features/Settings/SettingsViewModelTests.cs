using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Moq;

namespace AutoBrowser.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<IProtocolService> _mockProtocolService;
    private readonly Mock<IDefaultBrowserService> _mockDefaultBrowserService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IBrowserProvider> _mockBrowserProvider;
    private readonly Mock<IWindowsSearchInterceptorService> _mockWindowsSearchInterceptorService;

    public SettingsViewModelTests()
    {
        _mockProtocolService = new Mock<IProtocolService>();
        _mockDefaultBrowserService = new Mock<IDefaultBrowserService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockBrowserProvider = new Mock<IBrowserProvider>();
        _mockWindowsSearchInterceptorService = new Mock<IWindowsSearchInterceptorService>();
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(new AppSettings());
        _mockBrowserProvider.Setup(x => x.GetInstalledBrowsers()).Returns(new List<BrowserDefinition>());
    }

    [Fact]
    public void Constructor_InitializesSettings()
    {
        // Act
        var vm = new SettingsViewModel(
            _mockProtocolService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object,
            _mockBrowserProvider.Object,
            _mockWindowsSearchInterceptorService.Object);

        // Assert
        Assert.NotNull(vm.AvailableBrowsers);
    }


    [Fact]
    public void ShowPushNotifications_SetAndGet_WorksCorrectly()
    {
        var settings = new AppSettings { ShowPushNotifications = true };
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(settings);

        var vm = new SettingsViewModel(
            _mockProtocolService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object,
            _mockBrowserProvider.Object,
            _mockWindowsSearchInterceptorService.Object);

        Assert.True(vm.ShowPushNotifications);

        vm.ShowPushNotifications = false;
        Assert.False(vm.ShowPushNotifications);
        _mockSettingsService.Verify(x => x.SaveSettings(It.Is<AppSettings>(s => s.ShowPushNotifications == false)), Times.Once);
    }

    [Fact]
    public void InterceptWindowsSearch_EnableFailure_ResetsWithoutRecursion()
    {
        _mockWindowsSearchInterceptorService.Setup(x => x.EnableInterception()).Returns(false);

        var vm = new SettingsViewModel(
            _mockProtocolService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object,
            _mockBrowserProvider.Object,
            _mockWindowsSearchInterceptorService.Object);

        vm.Initialize();

        vm.InterceptWindowsSearch = true;

        Assert.False(vm.InterceptWindowsSearch);
        Assert.Equal("Failed to enable interception", vm.Status);
        _mockWindowsSearchInterceptorService.Verify(x => x.EnableInterception(), Times.Once);
        _mockWindowsSearchInterceptorService.Verify(x => x.DisableInterception(), Times.Never);
    }
}
