using AdonisUI.Controls;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MessagePack;
using NLog;
using RoSatGCS.Models;
using RoSatGCS.Utils.Navigation;
using RoSatGCS.Utils.ServiceManager;
using RoSatGCS.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RoSatGCS.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly string PathCacheSetting = "Cache/setting";


        public MainDataContext MainDataContext => MainDataContext.Instance;
        
        private WindowSettings? windowSettings;
        private WindowOnboardScheduler? windowOnboardScheduler;
        private WindowFunctionProperty? windowFunctionProperty;
        private ServiceManager? _serviceManager;

        private Page? _navigationSource;
        public Page? NavigationSource
        {
            get => _navigationSource;
            set => SetProperty(ref _navigationSource, value);
        }

        public ServiceManager? ServiceManager => _serviceManager;

        public SettingsModel SettingsModel => SettingsModel.Instance;

        
        public ICommand NavigateCommand { get; }
        public ICommand OpenWindowCommand { get; }
        public ICommand ClosingCommand { get; }
        public ICommand ClosedCommand { get; }
        public ICommand StartService { get; }
        public ICommand StopService { get; }
        public ICommand RestartService { get; }
        public ICommand OpenOnboardScheduler { get; }

        public MainWindowViewModel()
        {
            
            // Initialize MainDataContext
            MainDataContext.Instance.Load();

            Title = "Main View";


            // Pages
#if DEBUG
            NavigationSource = MainDataContext.PageCommand;
#else
            NavigationSource = MainDataContext.PageCommand;
#endif


            NavigateCommand = new RelayCommand<string>(OnNavigate);
            WeakReferenceMessenger.Default.Register<NavigationMessage>(this, OnNavigationMessage);
            
            // Commands
            OpenWindowCommand = new RelayCommand<object>(OnWindowOpen);
            ClosingCommand = new RelayCommand(OnClosing);
            ClosedCommand = new RelayCommand(OnClosed);
            StartService = new RelayCommand(OnStartService);
            StopService = new RelayCommand(OnStopService);
            RestartService = new RelayCommand(OnRestartService);
            OpenOnboardScheduler = new RelayCommand(() => OnWindowOpen("onboardscheduler"));

            _serviceManager = ServiceManager.Instance;
            _serviceManager.Start();

            _ = Update();

            if (File.Exists(PathCacheSetting))
            {
                using (var fileStream = File.OpenRead(PathCacheSetting))
                {
                    try
                    {
                        var temp = MessagePackSerializer.Deserialize<SettingsModel>(fileStream);

                        SettingsModel.Load(temp);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex);
                    }
                }
            }
        }

        private void OnNavigationMessage(object recipient, NavigationMessage message)
        {
           OnNavigate(message.Value);
        }

        private void OnNavigate(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri)) return;
            switch (uri)
            {
                case "archive":
                    NavigationSource = MainDataContext.PageArchive;
                    break;
                case "command":
                    NavigationSource = MainDataContext.PageCommand;
                    break;
                case "dashboard":
                    NavigationSource = MainDataContext.PageDashboard;
                    break;
                case "groundtrack":
                    NavigationSource = MainDataContext.PageGroundTrack;
                    break;
                case "scheduler":
                    NavigationSource = MainDataContext.PageScheduler;
                    break;
                case "fileshare":
                    NavigationSource = MainDataContext.PageFileShare;
                    break;
                default:
                    break;
            }
        }
        
        public void OnWindowOpen(object? arg)
        {
            if(arg == null) return;
            string uri = arg as string ?? string.Empty;

            switch (uri)
            {
                case "settings":
                    if(windowSettings == null)
                    {
                        windowSettings = new WindowSettings();
                        windowSettings.Closed += (a, b) => windowSettings = null;
                    }
                    windowSettings.ShowInTaskbar = false;
                    windowSettings.Show();
                    windowSettings.Focus();
                    break;
                case "onboardscheduler":
                    if (windowOnboardScheduler == null)
                    {
                        windowOnboardScheduler = new WindowOnboardScheduler();
                        windowOnboardScheduler.Closed += (a, b) => windowOnboardScheduler = null;
                    }
                    windowOnboardScheduler.ShowInTaskbar = false;
                    windowOnboardScheduler.Show();
                    windowOnboardScheduler.Focus();
                    break;
                default:
                    break;
            }
        }
        
        public void OnWindowOpenFunctionProperty(SatelliteCommandModel cmd, EventHandler handler)
        {
            if (cmd == null) return;
            if (windowFunctionProperty == null)
            {
                windowFunctionProperty = new WindowFunctionProperty(cmd);
                windowFunctionProperty.Closed += (a, b) => windowFunctionProperty = null;
                windowFunctionProperty.Closed += handler;
            }
            windowFunctionProperty.ShowInTaskbar = false;
            windowFunctionProperty.Show();
            windowFunctionProperty.Focus();
        }

        private void WindowFunctionProperty_Closed(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnWindowClose(object? arg)
        {
            if (arg == null) return;
            string uri = arg as string ?? string.Empty;
            switch (uri)
            {
                case "settings":
                    if (windowSettings != null)
                    {
                        windowSettings.Close();
                        windowSettings = null;
                    }
                    break;
                case "onboardscheduler":
                    if (windowOnboardScheduler != null)
                    {
                        windowOnboardScheduler.Close();
                        windowOnboardScheduler = null;
                    }
                    break;
                case "functionproperty":
                    if (windowFunctionProperty != null)
                    {
                        windowFunctionProperty.Close();
                        windowFunctionProperty = null;
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnClosing()
        {
            // Save MainDataContext
            MainDataContext.Instance.Save();

            /*     private Page? pageArchive;
             private Page? pageCommand;
             private Page? pageDashboard;
             private Page? pageGroundTrack;
             private Page? pageScheduler;*/
            PageCommandViewModel? vm = MainDataContext.Instance.GetPageCommandViewModel;
            if (vm != null)
            {
                vm.Closing.Execute(null);
            }
        }

        private void OnClosed()
        {
            Application.Current.Shutdown();
        }

        private void OnStartService()
        {
            ServiceManager?.Start();
        }

        private void OnStopService()
        {
            ServiceManager?.Stop();
        }

        private void OnRestartService()
        {
            ServiceManager?.Restart();
        }

        public DateTime CurrentTime
        {
            get => DateTime.Now;
        }

        private async Task Update()
        {
            while (true)
            {
                await Task.Delay(100);
                OnPropertyChanged(nameof(CurrentTime));
            }
        }

        

    }
}
