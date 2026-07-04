using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AutoBrowser.Models;
using AutoBrowser.Services;

namespace AutoBrowser.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IRuleService _ruleService;
    private readonly IProtocolService _protocolService;
    private readonly IDefaultBrowserService _defaultBrowserService;
    private readonly ISettingsService _settingsService;
    private RoutingRule? _selectedRule;
    private bool _isDarkTheme;
    private string _status = "Ready";

    public ObservableCollection<RoutingRule> Rules { get; } = [];

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public RoutingRule? SelectedRule
    {
        get => _selectedRule;
        set { _selectedRule = value; OnPropertyChanged(); }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            _isDarkTheme = value;
            ((App)System.Windows.Application.Current).ApplyTheme(value ? AppThemeMode.Dark : AppThemeMode.Light);
            Status = value ? "Switched to Dark theme" : "Switched to Light theme";
            OnPropertyChanged();
        }
    }

    public bool IsProtocolRegistered
    {
        get => _protocolService.IsProtocolRegistered();
        set
        {
            if (value)
            {
                _protocolService.RegisterProtocolHandler();
                Status = "autobrowser:// protocol registered";
            }
            else
            {
                _protocolService.UnregisterProtocolHandler();
                Status = "autobrowser:// protocol unregistered";
            }
            OnPropertyChanged();
        }
    }

    public bool IsDefaultBrowser
    {
        get => _defaultBrowserService.IsDefaultBrowser();
        set
        {
            if (value)
            {
                _defaultBrowserService.RegisterAsDefaultBrowser();
                Status = "Registered as default browser — select AutoBrowser in Settings > Default Apps";
            }
            else
            {
                _defaultBrowserService.UnregisterAsDefaultBrowser();
                Status = "Unregistered as default browser";
            }
            OnPropertyChanged();
        }
    }

    public ICommand AddRuleCommand { get; }
    public ICommand EditRuleCommand { get; }
    public ICommand DeleteRuleCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand LaunchUrlCommand { get; }

    public MainViewModel()
    {
        _ruleService = new RuleService();
        _protocolService = new ProtocolService();
        _defaultBrowserService = new DefaultBrowserService();
        _settingsService = new SettingsService();
        LoadRules();

        var app = (App)System.Windows.Application.Current;
        _isDarkTheme = app.CurrentThemeMode == AppThemeMode.Dark;

        AddRuleCommand = new RelayCommand(_ => AddRule());
        EditRuleCommand = new RelayCommand(_ => EditRule(), _ => SelectedRule is not null);
        DeleteRuleCommand = new RelayCommand(_ => DeleteRule(), _ => SelectedRule is not null);
        MoveUpCommand = new RelayCommand(_ => MoveUp(), _ => SelectedRule is not null);
        MoveDownCommand = new RelayCommand(_ => MoveDown(), _ => SelectedRule is not null);
        LaunchUrlCommand = new RelayCommand(_ => LaunchUrl());
    }

    private void LoadRules()
    {
        Rules.Clear();
        foreach (var rule in _ruleService.LoadRules())
            Rules.Add(rule);
    }

    public void SaveRules()
    {
        _ruleService.SaveRules([..Rules]);
    }

    private void AddRule()
    {
        var dialog = new RuleDialog();
        if (dialog.ShowDialog() == true)
        {
            Rules.Add(dialog.Rule);
            SelectedRule = dialog.Rule;
            SaveRules();
            Status = $"Rule \"{dialog.Rule.Name}\" added";
        }
    }

    private void EditRule()
    {
        if (SelectedRule is null) return;

        var index = Rules.IndexOf(SelectedRule);
        var dialog = new RuleDialog(SelectedRule);
        if (dialog.ShowDialog() == true)
        {
            Rules[index] = dialog.Rule;
            SelectedRule = dialog.Rule;
            SaveRules();
            Status = $"Rule \"{dialog.Rule.Name}\" updated";
        }
    }

    private void DeleteRule()
    {
        if (SelectedRule is null) return;
        var name = SelectedRule.Name;
        Rules.Remove(SelectedRule);
        SaveRules();
        Status = $"Rule \"{name}\" deleted";
    }

    private void MoveUp()
    {
        MoveRule(-1);
    }

    private void MoveDown()
    {
        MoveRule(1);
    }

    private void MoveRule(int direction)
    {
        if (SelectedRule is null) return;

        var index = Rules.IndexOf(SelectedRule);
        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= Rules.Count) return;

        Rules.Move(index, newIndex);
        SaveRules();
        Status = $"Rule \"{SelectedRule.Name}\" moved";
    }

    private void LaunchUrl()
    {
        var dialog = new InputDialog("Test URL", "Enter URL to test routing:", "https://");
        dialog.ShowDialog();
        var url = dialog.Result;
        if (string.IsNullOrWhiteSpace(url)) return;

        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        if (interceptor.TryRoute(url))
            Status = $"Routed: {url}";
        else
            Status = $"No match — opened in default browser: {url}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

