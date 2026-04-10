using CommunityToolkit.Mvvm.DependencyInjection;
using ISeeYou.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace ISeeYou
{
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            ConfigServices();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = Ioc.Default.GetService<HomePage>();
            
            if (_window != null)
                _window.Activate();
        }

        private void ConfigServices()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddSingleton<HomePage>();
            services.AddSingleton<HomePageViewModel>();

            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        }
    }
}
