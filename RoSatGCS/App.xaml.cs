using System;
using System.Configuration;
using System.Data;
using System.Windows;
using AdonisUI.Controls;
using Microsoft.Extensions.DependencyInjection;
using RoSatGCS.ViewModels;
using RoSatGCS.Views;

namespace RoSatGCS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(MainWindowViewModel)); // 종료까지 유지
            services.AddTransient(typeof(WindowSettingsViewModel));
            services.AddTransient(typeof(WindowTLEViewModel));
            services.AddTransient(typeof(WindowOnboardSchedulerViewModel));
            services.AddTransient(typeof(PageArchiveViewModel));
            services.AddTransient(typeof(PageCommandViewModel));
            services.AddTransient(typeof(PageDashboardViewModel));
            services.AddTransient(typeof(PageGroundTrackViewModel));
            services.AddTransient(typeof(PageSchedulerViewModel));
            services.AddTransient(typeof(PageFileShareViewModel));

            services.AddTransient(typeof(PaneCommandFileViewModel));
            return services.BuildServiceProvider();
        }
    }
}
