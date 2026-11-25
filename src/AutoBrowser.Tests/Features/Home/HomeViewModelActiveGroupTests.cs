using System.Collections.ObjectModel;
using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Moq;

namespace AutoBrowser.Tests.ViewModels;

public class HomeViewModelActiveGroupTests
{
    private readonly Mock<IRuleService> _ruleService = new();
    private readonly Mock<IDialogService> _dialogService = new();

    public HomeViewModelActiveGroupTests()
    {
        _ruleService.Setup(x => x.SaveGroups(It.IsAny<List<RuleGroup>>()));
        _dialogService.Setup(x => x.ShowAddRuleDialog()).Returns((RoutingRule?)null);
    }

    private HomeViewModel CreateViewModel(RuleGroup group)
    {
        _ruleService.Setup(x => x.LoadGroups()).Returns(new List<RuleGroup> { group });
        return new HomeViewModel(
            _ruleService.Object,
            new Mock<IDefaultBrowserService>().Object,
            new Mock<ISettingsService>().Object,
            _dialogService.Object,
            new UpdateService(),
            new Mock<IBrowserProvider>().Object,
            new Mock<INotificationService>().Object,
            new List<IBrowserLauncher>());
    }

    private static RuleGroup CreateGroup() => new()
    {
        Id = "1",
        Name = "Default",
        IsEnabled = true,
        Sequence = 1,
        Rules = new ObservableCollection<RoutingRule>()
    };

    [Fact]
    public void HasVisibleRules_RaisedWhenRuleAddedToActiveGroup()
    {
        var group = CreateGroup();
        var vm = CreateViewModel(group);
        Assert.False(vm.HasVisibleRules);

        Assert.PropertyChanged(vm, nameof(vm.HasVisibleRules),
            () => group.Rules.Add(new RoutingRule { Name = "Rule", UrlPattern = "example.com" }));

        Assert.True(vm.HasVisibleRules);
    }

    [Fact]
    public void HasVisibleRules_RaisedWhenLastRuleRemovedFromActiveGroup()
    {
        var group = CreateGroup();
        group.Rules.Add(new RoutingRule { Name = "Rule", UrlPattern = "example.com" });
        var vm = CreateViewModel(group);
        Assert.True(vm.HasVisibleRules);

        Assert.PropertyChanged(vm, nameof(vm.HasVisibleRules), () => group.Rules.RemoveAt(0));

        Assert.False(vm.HasVisibleRules);
    }

    [Fact]
    public void RenameGroup_UpdatesGroupThroughViewModel()
    {
        var group = CreateGroup();
        var vm = CreateViewModel(group);

        vm.RenameGroup(group, "  Renamed  ");

        Assert.Equal("Renamed", group.Name);
    }
}
