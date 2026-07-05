using AutoBrowser.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoBrowser.Views;

public partial class RuleTesterView : FluentWindow
{
    private static readonly SettingsService _settingsService = new();

    public string? Result { get; private set; }

    public RuleTesterView(string title, string prompt, string defaultValue = "")
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;

        var settings = _settingsService.LoadSettings();
        InputBox.Text = string.IsNullOrWhiteSpace(defaultValue) ? settings.LastTestUrl : defaultValue;

        Owner = System.Windows.Application.Current.MainWindow;
        InputBox.Focus();
        InputBox.SelectAll();
    }

    private void Ok_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Result = InputBox.Text;
        if (!string.IsNullOrWhiteSpace(Result))
        {
            var settings = _settingsService.LoadSettings();
            settings.LastTestUrl = Result;
            _settingsService.SaveSettings(settings);
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
