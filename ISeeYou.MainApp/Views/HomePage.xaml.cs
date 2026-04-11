using Microsoft.UI.Xaml;
using ISeeYou.MainApp.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace ISeeYou.MainApp.Views
{
    public sealed partial class HomePage : Window
    {
        public HomePageViewModel ViewModel { get; }

        public HomePage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetService<HomePageViewModel>();
        }
    }
}