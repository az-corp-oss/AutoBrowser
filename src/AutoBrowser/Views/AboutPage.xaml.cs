using System.Windows.Controls;
using AutoBrowser.ViewModels;

namespace AutoBrowser.Views;

public partial class AboutPage : Page
{
    public AboutPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
