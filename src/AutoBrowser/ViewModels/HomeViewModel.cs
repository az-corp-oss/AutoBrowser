using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using AutoBrowser.Helpers;
using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace AutoBrowser.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IRuleService _ruleService;
    private readonly IDefaultBrowserService _defaultBrowserService;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly UpdateService _updateService = new();
    private bool _isSyncing;
    private readonly Dictionary<RoutingRule, RuleGroup> _ruleGroupMap = new();

    public ObservableCollection<RuleGroup> Groups { get; } = [];
    public ObservableCollection<RoutingRule> Rules { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRule))]
    [NotifyPropertyChangedFor(nameof(HasSelectedGroup))]
    [NotifyPropertyChangedFor(nameof(SelectedRule))]
    [NotifyPropertyChangedFor(nameof(SelectedGroup))]
    [NotifyCanExecuteChangedFor(nameof(EditRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    private object? _selectedItem;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckForUpdate))]
    private bool _isCheckingUpdate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckForUpdate))]
    private bool _isDownloadingUpdate;

    [ObservableProperty]
    private string _updateStatus = "";

    public RuleGroup? SelectedGroup
    {
        get => SelectedItem as RuleGroup;
        set => SelectedItem = value;
    }

    public RoutingRule? SelectedRule
    {
        get => SelectedItem as RoutingRule;
        set => SelectedItem = value;
    }

    public bool HasSelectedRule => SelectedRule is not null;
    public bool HasSelectedGroup => SelectedGroup is not null;
    public bool CanCheckForUpdate => !IsCheckingUpdate && !IsDownloadingUpdate;

    public HomeViewModel(
        IRuleService ruleService,
        IDefaultBrowserService defaultBrowserService,
        ISettingsService settingsService,
        IDialogService dialogService)
    {
        _ruleService = ruleService;
        _defaultBrowserService = defaultBrowserService;
        _settingsService = settingsService;
        _dialogService = dialogService;

        LoadGroups();

        Groups.CollectionChanged += Groups_CollectionChanged;
        foreach (var group in Groups)
        {
            SubscribeToGroupEvents(group);
        }

        Rules.CollectionChanged += Rules_CollectionChanged;
        foreach (var rule in Rules)
        {
            rule.PropertyChanged += Rule_PropertyChanged;
        }
    }

    private void Groups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing) return;

        _isSyncing = true;
        try
        {
            if (e.OldItems is not null)
            {
                foreach (RuleGroup group in e.OldItems)
                {
                    UnsubscribeFromGroupEvents(group);
                    if (SelectedItem == group)
                    {
                        SelectedItem = null;
                    }
                    foreach (var rule in group.Rules)
                    {
                        rule.PropertyChanged -= Rule_PropertyChanged;
                        if (SelectedItem == rule)
                        {
                            SelectedItem = null;
                        }
                        Rules.Remove(rule);
                        _ruleGroupMap.Remove(rule);
                    }
                }
            }

            if (e.NewItems is not null)
            {
                foreach (RuleGroup group in e.NewItems)
                {
                    SubscribeToGroupEvents(group);
                    foreach (var rule in group.Rules)
                    {
                        rule.PropertyChanged += Rule_PropertyChanged;
                        Rules.Add(rule);
                        _ruleGroupMap[rule] = group;
                    }
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }

        SaveGroups();
    }

    private void SubscribeToGroupEvents(RuleGroup group)
    {
        group.PropertyChanged += Group_PropertyChanged;
        group.Rules.CollectionChanged += GroupRules_CollectionChanged;
    }

    private void UnsubscribeFromGroupEvents(RuleGroup group)
    {
        group.PropertyChanged -= Group_PropertyChanged;
        group.Rules.CollectionChanged -= GroupRules_CollectionChanged;
    }

    private void Group_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveGroups();
    }

    private void GroupRules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing) return;

        _isSyncing = true;
        try
        {
            if (e.OldItems is not null)
            {
                foreach (RoutingRule rule in e.OldItems)
                {
                    rule.PropertyChanged -= Rule_PropertyChanged;
                }
            }

            RebuildFlatRules();
        }
        finally
        {
            _isSyncing = false;
        }

        SaveGroups();
    }

    private void RebuildFlatRules()
    {
        // Unsubscribe all current rules first to avoid memory leaks
        foreach (var rule in Rules)
        {
            rule.PropertyChanged -= Rule_PropertyChanged;
        }

        Rules.Clear();
        _ruleGroupMap.Clear();

        foreach (var group in Groups)
        {
            foreach (var rule in group.Rules)
            {
                Rules.Add(rule);
                _ruleGroupMap[rule] = group;
                rule.PropertyChanged += Rule_PropertyChanged;
            }
        }
    }

    private void Rules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing) return;

        _isSyncing = true;
        try
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var rule in _ruleGroupMap.Keys)
                {
                    rule.PropertyChanged -= Rule_PropertyChanged;
                }
                if (SelectedItem is RoutingRule)
                {
                    SelectedItem = null;
                }
                foreach (var group in Groups)
                    group.Rules.Clear();
                _ruleGroupMap.Clear();
            }
            else
            {
                if (e.OldItems is not null)
                {
                    foreach (RoutingRule rule in e.OldItems)
                    {
                        rule.PropertyChanged -= Rule_PropertyChanged;
                        if (SelectedItem == rule)
                        {
                            SelectedItem = null;
                        }
                        if (_ruleGroupMap.TryGetValue(rule, out var group))
                        {
                            group.Rules.Remove(rule);
                            _ruleGroupMap.Remove(rule);
                        }
                    }
                }

                if (e.NewItems is not null)
                {
                    var targetGroup = Groups.FirstOrDefault();
                    if (targetGroup == null)
                    {
                        targetGroup = new RuleGroup 
                        { 
                            Id = UlidHelper.NewUlid(), 
                            Name = "Default", 
                            IsEnabled = true, 
                            Sequence = 1 
                        };
                        Groups.Add(targetGroup);
                    }

                    foreach (RoutingRule rule in e.NewItems)
                    {
                        rule.PropertyChanged += Rule_PropertyChanged;
                        targetGroup.Rules.Add(rule);
                        _ruleGroupMap[rule] = targetGroup;
                    }
                }

                if (e.Action == NotifyCollectionChangedAction.Move)
                {
                    foreach (var group in Groups)
                    {
                        var groupRulesOrdered = Rules.Where(r => _ruleGroupMap.TryGetValue(r, out var g) && g == group).ToList();
                        group.Rules.Clear();
                        foreach (var r in groupRulesOrdered)
                        {
                            group.Rules.Add(r);
                        }
                    }
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }

        SaveGroups();
    }

    private void Rule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveGroups();
    }

    private void LoadGroups()
    {
        _isSyncing = true;
        Groups.Clear();
        Rules.Clear();
        _ruleGroupMap.Clear();

        var groups = _ruleService.LoadGroups();
        foreach (var group in groups)
        {
            Groups.Add(group);
            foreach (var rule in group.Rules)
            {
                Rules.Add(rule);
                _ruleGroupMap[rule] = group;
            }
        }
        _isSyncing = false;
    }

    private void SaveGroups()
    {
        _ruleService.SaveGroups([..Groups]);
    }

    private void UpdateGroupSequences(RuleGroup group)
    {
        for (int i = 0; i < group.Rules.Count; i++)
        {
            group.Rules[i].Sequence = i + 1;
        }
    }

    [RelayCommand]
    private void AddRule()
    {
        var rule = _dialogService.ShowAddRuleDialog();
        if (rule != null)
        {
            var targetGroup = SelectedGroup ?? Groups.FirstOrDefault();
            if (targetGroup == null)
            {
                targetGroup = new RuleGroup 
                { 
                    Id = UlidHelper.NewUlid(), 
                    Name = "Default", 
                    IsEnabled = true, 
                    Sequence = 1 
                };
                Groups.Add(targetGroup);
            }

            _isSyncing = true;
            try
            {
                targetGroup.Rules.Add(rule);
                UpdateGroupSequences(targetGroup);
                Rules.Add(rule);
                _ruleGroupMap[rule] = targetGroup;
                rule.PropertyChanged += Rule_PropertyChanged;
            }
            finally
            {
                _isSyncing = false;
            }

            SelectedItem = rule;
            SaveGroups();
            Status = $"Rule \"{rule.Name}\" added";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void EditRule()
    {
        if (SelectedRule is null) return;
        
        var group = Groups.FirstOrDefault(g => g.Rules.Contains(SelectedRule));
        if (group == null) return;

        var index = group.Rules.IndexOf(SelectedRule);
        var oldRule = SelectedRule;
        var rule = _dialogService.ShowEditRuleDialog(oldRule);
        if (rule != null)
        {
            oldRule.PropertyChanged -= Rule_PropertyChanged;
            _isSyncing = true;
            try
            {
                var flatIndex = Rules.IndexOf(oldRule);
                if (flatIndex >= 0)
                {
                    Rules[flatIndex] = rule;
                }
                group.Rules[index] = rule;
                _ruleGroupMap.Remove(oldRule);
                _ruleGroupMap[rule] = group;
                rule.PropertyChanged += Rule_PropertyChanged;
            }
            finally
            {
                _isSyncing = false;
            }

            SelectedItem = rule;
            SaveGroups();
            Status = $"Rule \"{rule.Name}\" updated";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void DeleteRule()
    {
        if (SelectedRule is null) return;
        var name = SelectedRule.Name;
        var rule = SelectedRule;
        
        var group = Groups.FirstOrDefault(g => g.Rules.Contains(rule));
        if (group != null)
        {
            rule.PropertyChanged -= Rule_PropertyChanged;
            _isSyncing = true;
            try
            {
                group.Rules.Remove(rule);
                UpdateGroupSequences(group);
                Rules.Remove(rule);
                _ruleGroupMap.Remove(rule);
            }
            finally
            {
                _isSyncing = false;
            }

            SelectedItem = null;
            SaveGroups();
            Status = $"Rule \"{name}\" deleted";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void MoveUp() => MoveRule(-1);

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void MoveDown() => MoveRule(1);

    private void MoveRule(int direction)
    {
        if (SelectedRule is null) return;
        
        var group = Groups.FirstOrDefault(g => g.Rules.Contains(SelectedRule));
        if (group == null) return;

        var index = group.Rules.IndexOf(SelectedRule);
        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= group.Rules.Count) return;

        _isSyncing = true;
        try
        {
            group.Rules.Move(index, newIndex);
            UpdateGroupSequences(group);
            
            var flatIndex = Rules.IndexOf(SelectedRule);
            var newFlatIndex = flatIndex + direction;
            if (newFlatIndex >= 0 && newFlatIndex < Rules.Count)
            {
                Rules.Move(flatIndex, newFlatIndex);
            }
        }
        finally
        {
            _isSyncing = false;
        }

        SaveGroups();
        Status = $"Rule \"{SelectedRule.Name}\" moved";
    }

    [RelayCommand]
    private void LaunchUrl()
    {
        var dialog = new RuleTesterView("Test URL", "Enter URL to test routing:");
        dialog.ShowDialog();
        var url = dialog.Result;
        if (string.IsNullOrWhiteSpace(url)) return;

        var settings = _settingsService.LoadSettings();
        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var result = interceptor.TryRoute(url, settings.FallbackBrowserPath);
        if (result.Type == RouteResultType.Forwarded)
        {
            Status = $"Routed via {result.BrowserDisplayName}: {url}";
            if (settings.ShowPushNotifications)
            {
                var msg = string.IsNullOrEmpty(result.RuleName) ? $"Routed via {result.BrowserDisplayName}:\n{url}" : $"Routed via {result.BrowserDisplayName} ({result.RuleName}):\n{url}";
                ShowNotification("AutoBrowser", msg);
            }
        }
        else if (result.Type == RouteResultType.Dropped)
        {
            Status = $"Dropped: {url}";
            if (settings.ShowPushNotifications)
            {
                var msg = string.IsNullOrEmpty(result.RuleName) ? $"URL dropped by matching rule:\n{url}" : $"URL dropped by matching rule ({result.RuleName}):\n{url}";
                ShowNotification("AutoBrowser", msg);
            }
        }
        else
        {
            Status = $"No match: {url}";
            if (settings.ShowPushNotifications)
            {
                ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
            }
        }
    }

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        if (IsCheckingUpdate || IsDownloadingUpdate) return;
        IsCheckingUpdate = true;
        UpdateStatus = "Checking for updates...";
        Status = UpdateStatus;
        Log.Information("Manual update check starting");

        try
        {
            var release = await _updateService.CheckForUpdateAsync();
            if (release is null)
            {
                UpdateStatus = "No release info available (no releases or offline).";
                Status = UpdateStatus;
                Log.Debug("Manual update check: no release info");
                return;
            }

            if (!release.IsNewer)
            {
                UpdateStatus = $"You're up to date (v{release.Version}).";
                Status = UpdateStatus;
                Log.Debug("Manual update check: up to date (v{Version})", release.Version);
                return;
            }

            Log.Debug("Manual update check: v{Version} available", release.Version);
            await ShowUpdateDialogAsync(release);
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Update failed: {ex.Message}";
            Status = UpdateStatus;
            Log.Error(ex, "Manual update check failed");
        }
        finally
        {
            IsCheckingUpdate = false;
            IsDownloadingUpdate = false;
        }
    }

    private async Task ShowUpdateDialogAsync(ReleaseInfo release)
    {
        var dialog = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Update Available",
            Content = $"Version {release.Version} is available.\n\nCurrent: {typeof(UpdateService).Assembly.GetName().Version}\n\nDownload and install?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            Width = 500,
            MinWidth = 500
        };
        dialog.Owner = Application.Current.MainWindow;
        var result = await dialog.ShowDialogAsync();
        if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded)
        {
            Application.Current.MainWindow.Focus();
        }

        Log.Information("Update dialog result: {Result} for v{Version}", result, release.Version);
        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) return;

        IsDownloadingUpdate = true;
        UpdateStatus = "Downloading update...";
        Status = UpdateStatus;

        var progress = new Progress<double>(p =>
        {
            var pct = (int)(p * 100);
            UpdateStatus = $"Downloading... {pct}%";
            Status = UpdateStatus;
        });

        await _updateService.DownloadAndUpdateAsync(release, progress);
    }

    private static void ShowNotification(string title, string message)
    {
        try
        {
            var icon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""),
                Visible = true
            };
            icon.ShowBalloonTip(3000, title, message, ToolTipIcon.Warning);
            
            // Keep icon alive for balloon tip to render, then dispose
            _ = Task.Delay(4000).ContinueWith(_ =>
            {
                try
                {
                    icon.Visible = false;
                    icon.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to dispose notification icon");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to show notification");
        }
    }
}
