using System.Windows;
using System.Windows.Controls;
using AutoBrowser.Helpers;
using AutoBrowser.Models;
using Serilog;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoBrowser.Views;

public partial class RuleEditorView : FluentWindow
{
    public RoutingRule Rule { get; private set; }

    public RuleEditorView()
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
        Rule = new RoutingRule();
        Owner = Application.Current.MainWindow;

        ActionCombo.ItemsSource = Enum.GetValues(typeof(RoutingAction));
        ActionCombo.SelectedItem = RoutingAction.Forward;

        var browsers = BrowserDefinition.GetKnownBrowsers();
        Log.Information("RuleEditorView: Found {Count} browsers", browsers.Count);
        BrowserCombo.ItemsSource = browsers;
        if (browsers.Count > 0)
            BrowserCombo.SelectedIndex = 0;

        Loaded += (_, _) => NameBox.Focus();
    }

    public RuleEditorView(RoutingRule existing) : this()
    {
        NameBox.Text = existing.Name;
        PatternBox.Text = existing.UrlPattern;
        ActionCombo.SelectedItem = existing.Action;
        BrowserPathBox.Text = existing.BrowserPath;

        var browsers = BrowserCombo.ItemsSource as List<BrowserDefinition>;
        var match = browsers?.FirstOrDefault(b =>
            b.ExecutablePath.Equals(existing.BrowserPath, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            BrowserCombo.SelectedItem = match;
    }

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ActionCombo == null || BrowserComboLabel == null || BrowserCombo == null || CustomPathLabel == null || CustomPathGrid == null)
            return;

        if (ActionCombo.SelectedItem is RoutingAction action)
        {
            var isForward = action == RoutingAction.Forward;
            var visibility = isForward ? Visibility.Visible : Visibility.Collapsed;

            BrowserComboLabel.Visibility = visibility;
            BrowserCombo.Visibility = visibility;
            CustomPathLabel.Visibility = visibility;
            CustomPathGrid.Visibility = visibility;
        }
    }

    private void BrowserCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrowserCombo.SelectedItem is BrowserDefinition browser)
            BrowserPathBox.Text = browser.ExecutablePath;
    }

    private void BrowseBrowser(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select Browser Executable"
        };

        if (dialog.ShowDialog() == true)
            BrowserPathBox.Text = dialog.FileName;
    }

    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Ok_Click: NameBox.Text='{Name}', PatternBox.Text='{Pattern}', BrowserPathBox.Text='{Path}', Action='{Action}'",
            NameBox.Text, PatternBox.Text, BrowserPathBox.Text, ActionCombo.SelectedItem);

        var selectedAction = ActionCombo.SelectedItem is RoutingAction action ? action : RoutingAction.Forward;

        if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(PatternBox.Text))
        {
            Log.Warning("Ok_Click: Validation failed - empty name or pattern");
            await MessageBoxHelper.ShowAsync("Validation", "Name and URL pattern are required.");
            return;
        }

        if (selectedAction == RoutingAction.Forward && string.IsNullOrWhiteSpace(BrowserPathBox.Text))
        {
            Log.Warning("Ok_Click: Validation failed - empty browser path for Forward action");
            await MessageBoxHelper.ShowAsync("Validation", "Browser path is required for Forward action.");
            return;
        }

        var (isValid, error) = RoutingRule.ValidatePattern(PatternBox.Text.Trim());
        if (!isValid)
        {
            Log.Warning("Ok_Click: Invalid pattern - {Error}", error);
            await MessageBoxHelper.ShowAsync("Invalid URL Pattern", error ?? "Invalid pattern.");
            return;
        }

        Rule.Name = NameBox.Text.Trim();
        Rule.UrlPattern = PatternBox.Text.Trim();
        Rule.Action = selectedAction;

        if (selectedAction == RoutingAction.Forward)
        {
            Rule.BrowserPath = BrowserPathBox.Text.Trim();
            if (BrowserCombo.SelectedItem is BrowserDefinition browser)
            {
                Rule.BrowserArguments = browser.ArgumentsTemplate;
            }
            else
            {
                Rule.BrowserArguments = "{url}";
            }
        }
        else
        {
            Rule.BrowserPath = string.Empty;
            Rule.BrowserArguments = string.Empty;
        }

        Log.Information("Ok_Click: Rule saved - Name={Name}, Pattern={Pattern}, Action={Action}, Path={Path}",
            Rule.Name, Rule.UrlPattern, Rule.Action, Rule.BrowserPath);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
