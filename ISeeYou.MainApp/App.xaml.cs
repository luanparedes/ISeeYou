using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using ISeeYou.MainApp.ViewModels;
using ISeeYou.Core.Services;
using ISeeYou.Services;
using ISeeYou.MainApp.Views;

namespace ISeeYou.MainApp
{
    public partial class App : Application
    {
        private Window? _window;
        private IAppInfo _appInfo;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            ConfigServices();

            _appInfo = Ioc.Default.GetRequiredService<IAppInfo>();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = Ioc.Default.GetService<HomePage>();
            
            if (_window != null)
            {
                _window.Title = _appInfo.GetAppFullNameVersion();
                _window.Activate();

                CenterWindow();
            }
        }

        /// <summary>
        /// Configures and registers application services with the dependency injection container.
        /// </summary>
        /// <remarks>This method sets up the service provider by adding required services and view models
        /// as singletons. It should be called during application initialization to ensure that dependencies are
        /// available throughout the application's lifetime.</remarks>
        private void ConfigServices()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddSingleton<HomePage>();
            services.AddSingleton<HomePageViewModel>();
            services.AddSingleton<ILogService, LogServiceApplication>();
            services.AddSingleton<IAppInfo, AppInfo>();

            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        }

        /// <summary>
        /// Center the window to always open in the center of the screen.
        /// </summary>
        private void CenterWindow()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var centerX = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
            var centerY = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;

            appWindow.Move(new PointInt32(centerX, centerY));
        }
    }
}