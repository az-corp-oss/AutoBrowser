using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoBrowser.Models;

public partial class RuleGroup : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private int _sequence;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private ObservableCollection<RoutingRule> _rules = [];
}
