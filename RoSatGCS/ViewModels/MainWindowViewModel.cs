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

        private WindowSettings? windowSettings;
        private ServiceManager? _serviceManager;

        private Page? pageArchive;
        private Page? pageCommand;
        private Page? pageDashboard;
        private Page? pageGroundTrack;
        private Page? pageScheduler;
        private Page? pageFileShare;

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

        public MainWindowViewModel()
        {
            Title = "Main View";

            //pageDashboard = new PageDashboard();
            pageCommand = new PageCommand();
            pageFileShare = new PageFileShare();
            NavigationSource = pageCommand;
            NavigateCommand = new RelayCommand<string>(OnNavigate);
            WeakReferenceMessenger.Default.Register<NavigationMessage>(this, OnNavigationMessage);

            OpenWindowCommand = new RelayCommand<string>(OnWindowOpen);
            ClosingCommand = new RelayCommand(OnClosing);
            ClosedCommand = new RelayCommand(OnClosed);
            StartService = new RelayCommand(OnStartService);
            StopService = new RelayCommand(OnStopService);
            RestartService = new RelayCommand(OnRestartService);

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
                    if (pageArchive == null)
                    {
                        pageArchive = new PageArchive();
                    }
                    NavigationSource = pageArchive;
                    break;
                case "command":
                    if (pageCommand == null)
                    {
                        pageCommand = new PageCommand();
                    }
                    NavigationSource = pageCommand;
                    break;
                case "dashboard":
                    if(pageDashboard == null) {
                        pageDashboard = new PageDashboard();
                    }
                    NavigationSource = pageDashboard;
                    break;
                case "groundtrack":
                    if (pageGroundTrack == null)
                    {
                        pageGroundTrack = new PageGroundTrack();
                    }
                    NavigationSource = pageGroundTrack;
                    break;
                case "scheduler":
                    if (pageScheduler == null)
                    {
                        pageScheduler = new PageScheduler();
                    }
                    NavigationSource = pageScheduler;
                    break;
                case "fileshare":
                    if (pageFileShare == null)
                    {
                        pageFileShare = new PageFileShare();
                    }
                    NavigationSource = pageFileShare;
                    break;
                default:
                    break;
            }
        }
        
        private void OnWindowOpen(string? uri)
        {
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
                default:
                    break;
            }
        }

        private void OnClosing()
        {
            /*     private Page? pageArchive;
             private Page? pageCommand;
             private Page? pageDashboard;
             private Page? pageGroundTrack;
             private Page? pageScheduler;*/
            if (pageCommand != null && pageCommand.DataContext is PageCommandViewModel vm)
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
