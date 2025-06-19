using CommunityToolkit.Mvvm.ComponentModel;
using RoSatGCS.Utils.Timer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Utils.ServiceManager
{
    public class ServiceManager : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Lazy<ServiceManager> _instance = new(() => new ServiceManager());

        public static ServiceManager Instance => _instance.Value;

        private ServiceManager()
        {
            using ServiceController service = new(_serviceName);
            if (service.Status == ServiceControllerStatus.Running)
            {
                _started = true;
                _state = ServiceState.Started;
                OnPropertyChanged(nameof(State));
            }
            else if (service.Status == ServiceControllerStatus.Stopped)
            {
                _started = false;
                _state = ServiceState.Stopped;
                OnPropertyChanged(nameof(State));
            }

            TimeUpdateManager.Instance.Subscribe(StateManager);
        }

        ~ServiceManager() { TimeUpdateManager.Instance.Unsubscribe(StateManager); }

        public enum ServiceState
        {
            Starting, Started, Stopping, Stopped, Restart
        }

        private readonly static int _timeout = 3000;
        private readonly static string _serviceName = "RoSatProcessor";
        private readonly object _lock = new();
        private volatile bool _started = false;
        private volatile ServiceState _state = ServiceState.Stopped;
        public bool Started => _started;
        public ServiceState State => _state;
        
        public void Start()
        {
            lock (_lock)
            {
                if (_state == ServiceState.Stopped)
                {
                    _state = ServiceState.Starting;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_state == ServiceState.Started)
                {
                    _state = ServiceState.Stopping;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        public void Restart()
        {
            lock (_lock)
            {
                _state = ServiceState.Restart;
                OnPropertyChanged(nameof(State));
            }
        }


        private void StateManager()
        {
            lock (_lock)
            {
                switch (_state)
                {
                    case ServiceState.Starting:
                        if (!_started)
                            StartService();
                        break;
                    case ServiceState.Stopping:
                        if (_started)
                            StopService();
                        break;
                    case ServiceState.Restart:
                        RestartService();
                        break;
                }
            }
        }

        private void StartService()
        {
            using ServiceController service = new(_serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(_timeout);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    lock (_lock)
                    {
                        _started = true;
                        _state = ServiceState.Started;
                        OnPropertyChanged(nameof(State));
                    }
                    return;
                }
                
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);

                lock (_lock)
                {
                    _started = true;
                    _state = ServiceState.Started;
                    OnPropertyChanged(nameof(State));
                }
            }
            catch (System.ServiceProcess.TimeoutException e)
            {
                Logger.Error($"Failed to start service: {e.Message}");
            }
            catch (System.Exception e)
            {
                Logger.Error($"Unexpected error while starting service: {e.Message}");
            }
        }

        private void StopService()
        {
            if(_started == false) { return; }

            using ServiceController service = new(_serviceName);
            try
            {
                TimeSpan timeout;
                if (service.Status == ServiceControllerStatus.Running)
                {
                    timeout = TimeSpan.FromMilliseconds(_timeout);
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                    lock (_lock)
                    {
                        _started = false;
                        _state = ServiceState.Stopped;
                        OnPropertyChanged(nameof(State));
                    }
                }
                else if (service.Status == ServiceControllerStatus.Stopped)
                {
                    lock (_lock)
                    {
                        _started = false;
                        _state = ServiceState.Stopped;
                        OnPropertyChanged(nameof(State));
                    }
                }

            }
            catch (System.ServiceProcess.TimeoutException e)
            {
                Logger.Error($"Failed to stop service: {e.Message}");
            }
            catch (System.Exception e)
            {
                Logger.Error($"Unexpected error while stopping service: {e.Message}");
            }
        }

        private void RestartService()
        {
            using ServiceController service = new(_serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(_timeout);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                    lock (_lock)
                    {
                        _started = false;
                        _state = ServiceState.Stopped;
                        OnPropertyChanged(nameof(State));
                    }
                }

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);

                lock (_lock)
                {
                    _started = true;
                    _state = ServiceState.Started;
                    OnPropertyChanged(nameof(State));
                }

            }
            catch (System.ServiceProcess.TimeoutException e)
            {
                Logger.Error($"Failed to restart service: {e.Message}");
            }
            catch (System.Exception e)
            {
                Logger.Error($"Unexpected error while restarting service: {e.Message}");
            }
        }
    }
}
