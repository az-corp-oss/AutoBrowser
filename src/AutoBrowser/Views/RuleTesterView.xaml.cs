using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoBrowser.Views;

public partial class RuleTesterView : FluentWindow
{
    public string? Result { get; private set; }

    public RuleTesterView(string title, string prompt, string defaultValue = "")
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = defaultValue;
        Owner = System.Windows.Application.Current.MainWindow;
        InputBox.Focus();
        InputBox.SelectAll();
    }

    private void Ok_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Result = InputBox.Text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
