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

    public SettingsViewModelTests()
    {
        _mockProtocolService = new Mock<IProtocolService>();
        _mockDefaultBrowserService = new Mock<IDefaultBrowserService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockBrowserProvider = new Mock<IBrowserProvider>();
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
            _mockBrowserProvider.Object);

        // Assert
        Assert.NotNull(vm.AvailableBrowsers);
        // ... (remaining tests)
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
            _mockBrowserProvider.Object);

        Assert.True(vm.ShowPushNotifications);

        vm.ShowPushNotifications = false;
        Assert.False(vm.ShowPushNotifications);
        _mockSettingsService.Verify(x => x.SaveSettings(It.Is<AppSettings>(s => s.ShowPushNotifications == false)), Times.Once);
    }
}
