using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AutoBrowser.Models;
using AutoBrowser.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;

namespace AutoBrowser.Views;

public partial class GroupSidebarView : UserControl
{
    private const string CardName = "GroupCard";
    private const string EditorName = "GroupNameEdit";
    private const string LabelName = "GroupNameText";

    public GroupSidebarView()
    {
        InitializeComponent();
    }

    private void GroupNameText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not FrameworkElement element) return;

        StartEditing(element.DataContext as RuleGroup, FindAncestorNamed(element, CardName));
        e.Handled = true;
    }

    private void GroupList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F2 || sender is not ListBox list || list.SelectedItem is not RuleGroup group) return;

        if (list.ItemContainerGenerator.ContainerFromItem(group) is not DependencyObject container)
        {
            list.ScrollIntoView(group);
            container = list.ItemContainerGenerator.ContainerFromItem(group) as DependencyObject;
        }

        if (container is null) return;

        StartEditing(group, FindNamed(container, CardName) as Border);
        e.Handled = true;
    }

    private void GroupNameEdit_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box) return;

        if (e.Key == Key.Enter)
        {
            Commit(box);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            HideEditor(box);
            e.Handled = true;
        }
    }

    private void GroupNameEdit_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.Visibility == Visibility.Visible)
        {
            Commit(box);
        }
    }

    private void StartEditing(RuleGroup? group, DependencyObject? card)
    {
        if (group is null || card is null) return;
        if (FindNamed(card, EditorName) is not TextBox box) return;

        box.Text = group.Name;
        SetEditorVisibility(card, true);
        box.Focus();
        box.SelectAll();
    }

    private void Commit(TextBox box)
    {
        if (box.DataContext is RuleGroup group && DataContext is HomeViewModel vm)
        {
            vm.RenameGroup(group, box.Text);
        }

        HideEditor(box);
    }

    private static void HideEditor(TextBox box)
    {
        if (VisualTreeHelper.GetParent(box) is DependencyObject parent)
        {
            SetEditorVisibility(parent, false);
        }
    }

    private static void SetEditorVisibility(DependencyObject root, bool editing)
    {
        if (FindNamed(root, EditorName) is TextBox box)
        {
            box.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
        }

        if (FindNamed(root, LabelName) is FrameworkElement label)
        {
            label.Visibility = editing ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static FrameworkElement? FindAncestorNamed(DependencyObject node, string name)
    {
        DependencyObject? current = node;
        while (current is not null)
        {
            if (current is FrameworkElement element && element.Name == name) return element;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static FrameworkElement? FindNamed(DependencyObject root, string name)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement element && element.Name == name) return element;

            var nested = FindNamed(child, name);
            if (nested is not null) return nested;
        }

        return null;
    }
}
