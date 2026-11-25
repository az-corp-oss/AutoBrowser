using System.Windows;
using System.Windows.Input;

namespace AutoBrowser.Views;

public partial class RulesListView : UserControl
{
    public RulesListView()
    {
        InitializeComponent();
    }

    private void RuleBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src &&
            (src is System.Windows.Controls.CheckBox || IsDescendantOf<System.Windows.Controls.CheckBox>(src, sender as DependencyObject)))
        {
            return;
        }

        if (sender is FrameworkElement fe && DataContext is ViewModels.HomeViewModel vm)
        {
            vm.SelectedItem = fe.DataContext;

            if (e.ClickCount == 2 && vm.SelectedRule is not null)
            {
                vm.EditRuleCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private static bool IsDescendantOf<T>(DependencyObject node, DependencyObject? root) where T : DependencyObject
    {
        while (node is not null && node != root)
        {
            if (node is T) return true;
            node = System.Windows.Media.VisualTreeHelper.GetParent(node);
        }
        return false;
    }
}
