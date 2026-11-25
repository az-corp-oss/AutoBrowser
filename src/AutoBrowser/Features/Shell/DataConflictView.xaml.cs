using System.Windows;
using AutoBrowser.Models;
using Wpf.Ui.Appearance;

namespace AutoBrowser.Views;

public partial class DataConflictView
{
    public DataConflictView(IReadOnlyList<DataConflict> conflicts, string targetName, string sourceName)
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
        Conflicts = conflicts;
        ConflictList.ItemsSource = conflicts;
        DescriptionText.Text = $"Choose which version to keep in {targetName}. The version from {sourceName} is the other side.";
        Owner = Application.Current.MainWindow;
    }

    public IReadOnlyList<DataConflict> Conflicts { get; }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
