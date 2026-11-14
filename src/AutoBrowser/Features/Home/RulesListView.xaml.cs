using System.Globalization;
using System.Windows;
using System.Windows.Data;
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

    private void GroupBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && DataContext is ViewModels.HomeViewModel vm)
        {
            vm.SelectedItem = fe.DataContext;
            e.Handled = true;
        }
    }

    private void CollapseButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
    }
}

public class BoolToRotationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? 0.0 : -90.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class EqualityToBooleanConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2) return false;
        return Equals(values[0], values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
