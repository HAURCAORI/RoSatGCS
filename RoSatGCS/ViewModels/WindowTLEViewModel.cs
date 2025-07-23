using AdonisUI.Controls;
using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Utils.Exception;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Utils.Satellites.TLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace RoSatGCS.ViewModels
{
    internal class WindowTLEViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string WindowName = "TLE Viewer";
        private string _windowTitle = WindowName;

        private TLE? _tleData;
        private string _tleString = "";
        private char[] classList = ['U', 'C', 'S'];

        private string _satelliteName = "";
        private string _noradId = "";
        private string _intlDesignator = "";
        private char _class = 'U';
        private string _epoch = "";
        private string _setNumber = "";
        private string _inclination = "";
        private string _raan = "";
        private string _eccentricity = "";
        private string _argPerigee = "";
        private string _meanAnomaly = "";
        private string _meanMotion = "";
        private string _revAtEpoch = "";
        private string _meanMotionDt = "";
        private string _meanMotionDt2 = "";
        private string _bstar = "";
        private bool _ignoreChecksum = false;
        private bool _isChanged = false;
        private bool _isInitialized = false;

        public string WindowTitle
        {
            get => IsChanged ? _windowTitle + " *" : _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public TLE? TLEData { get => _tleData; set => _tleData = value; }
        public string TLEString
        {
            get => _tleString;
            set
            {
                if (SetProperty(ref _tleString, value) && _isInitialized)
                {
                    IsChanged = true;
                }

                _isInitialized = true;
            }
        }

        public char[] ClassList { get => classList; set => SetProperty(ref classList, value); }
        public string SatelliteName { get => _satelliteName; set => SetProperty(ref _satelliteName, value); }
        public string NoradId { get => _noradId; set => SetProperty(ref _noradId, value); }
        public string IntlDesignator { get => _intlDesignator; set => SetProperty(ref _intlDesignator, value); }
        public char Class { get => _class; set => SetProperty(ref _class, value); }
        public string Epoch { get => _epoch; set => SetProperty(ref _epoch, value); }
        public string SetNumber { get => _setNumber; set => SetProperty(ref _setNumber, value); }
        public string Inclination { get => _inclination; set => SetProperty(ref _inclination, value); }
        public string RAAN { get => _raan; set => SetProperty(ref _raan, value); }
        public string Eccentricity { get => _eccentricity; set => SetProperty(ref _eccentricity, value); }
        public string ArgPerigee { get => _argPerigee; set => SetProperty(ref _argPerigee, value); }
        public string MeanAnomaly { get => _meanAnomaly; set => SetProperty(ref _meanAnomaly, value); }
        public string MeanMotion { get => _meanMotion; set => SetProperty(ref _meanMotion, value); }
        public string RevAtEpoch { get => _revAtEpoch; set => SetProperty(ref _revAtEpoch, value); }
        public string MeanMotionDt { get => _meanMotionDt; set => SetProperty(ref _meanMotionDt, value); }
        public string MeanMotionDt2 { get => _meanMotionDt2; set => SetProperty(ref _meanMotionDt2, value); }
        public string BStar { get => _bstar; set => SetProperty(ref _bstar, value); }
        public bool IgnoreChecksum { get => _ignoreChecksum; set => SetProperty(ref _ignoreChecksum, value); }
        public bool IsChanged
        {
            get => _isChanged;
            set
            {
                if (SetProperty(ref _isChanged, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                }
                if (OnSaveCommand is RelayCommand command)
                {
                    command.NotifyCanExecuteChanged();
                }
            }
        }
        public bool IsInitialized { get => _isInitialized; set => SetProperty(ref _isInitialized, value); }


        public ICommand TLE2Value { get; }
        public ICommand Value2TLE { get; }
        public ICommand OnSaveCommand { get; }
        public ICommand OnCloseCommand { get; }

        public WindowTLEViewModel()
        {
            // Initialize properties and commands here
            TLE2Value = new RelayCommand(OnTLE2Value);
            Value2TLE = new RelayCommand(OnValue2TLE);

            OnSaveCommand = new RelayCommand<AdonisWindow>(OnSave);
            OnCloseCommand = new RelayCommand<AdonisWindow>(OnClose);
        }


        public void Initialize(TLE tle)
        {
            TLEData = tle;
            if (TLEData != null)
            {
                TLEString = TLEData.ToString();
                OnTLE2Value();
            }
        }

        private void OnTLE2Value()
        {
            try
            {
                TLEData = new TLE(TLEString, IgnoreChecksum);
                SatelliteName = TLEData.SatelliteName;
                NoradId = TLEData.NoradId.ToString();
                IntlDesignator = TLEData.IntlDesignator;
                Class = TLEData.Class;
                Epoch = TLEData.OrbitalElements.Epoch.ToTime().ToString();
                SetNumber = TLEData.SetNumber.ToString();
                Inclination = TLEData.OrbitalElements.InclinationDeg.ToString();
                RAAN = TLEData.OrbitalElements.RAANodeDeg.ToString();
                Eccentricity = TLEData.OrbitalElements.Eccentricity.ToString();
                ArgPerigee = TLEData.OrbitalElements.ArgPerigeeDeg.ToString();
                MeanAnomaly = TLEData.OrbitalElements.MeanAnomalyDeg.ToString();
                MeanMotion = TLEData.OrbitalElements.MeanMotion.ToString();
                RevAtEpoch = TLEData.RevAtEpoch.ToString();
                MeanMotionDt = TLEData.MeanMotionDt.ToString();
                MeanMotionDt2 = TLEData.MeanMotionDt2.ToString();
                BStar = TLEData.BStar.ToString();
            }
            catch (SatellitesTleException ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message, TranslationSource.Instance["sError"]
                    , System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message, TranslationSource.Instance["sError"]
                    , System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error));
            }
        }
        private void OnValue2TLE()
        {
            try
            {
                string sat = SatelliteName;
                if (string.IsNullOrWhiteSpace(sat))
                {
                    throw new SatellitesTleException("Empty Satellite Name");
                }

                if (string.IsNullOrWhiteSpace(NoradId) || !int.TryParse(NoradId, out _))
                {
                    throw new SatellitesTleException("Invalid NORAD ID");
                }

                if (string.IsNullOrWhiteSpace(IntlDesignator))
                {
                    throw new SatellitesTleException("Empty International Designator");
                }

                if (string.IsNullOrWhiteSpace(Epoch) || !DateTime.TryParse(Epoch, out _))
                {
                    throw new SatellitesTleException("Invalid Epoch Date");
                }

                if (string.IsNullOrWhiteSpace(SetNumber) || !int.TryParse(SetNumber, out _))
                {
                    throw new SatellitesTleException("Invalid Set Number");
                }

                if (string.IsNullOrWhiteSpace(Inclination) || !double.TryParse(Inclination, out _))
                {
                    throw new SatellitesTleException("Invalid Inclination");
                }

                if (string.IsNullOrWhiteSpace(RAAN) || !double.TryParse(RAAN, out _))
                {
                    throw new SatellitesTleException("Invalid RAAN");
                }

                if (string.IsNullOrWhiteSpace(Eccentricity) || !double.TryParse(Eccentricity, out _))
                {
                    throw new SatellitesTleException("Invalid Eccentricity");
                }

                if (string.IsNullOrWhiteSpace(ArgPerigee) || !double.TryParse(ArgPerigee, out _))
                {
                    throw new SatellitesTleException("Invalid Argument of Perigee");
                }

                if (string.IsNullOrWhiteSpace(MeanAnomaly) || !double.TryParse(MeanAnomaly, out _))
                {
                    throw new SatellitesTleException("Invalid Mean Anomaly");
                }

                if (string.IsNullOrWhiteSpace(MeanMotion) || !double.TryParse(MeanMotion, out _))
                {
                    throw new SatellitesTleException("Invalid Mean Motion");
                }

                if (string.IsNullOrWhiteSpace(RevAtEpoch) || !int.TryParse(RevAtEpoch, out _))
                {
                    throw new SatellitesTleException("Invalid Revolution at Epoch");
                }


                string line1 = TLE.GenerateLine1(int.Parse(NoradId), Class, IntlDesignator, DateTime.Parse(Epoch),
                    double.Parse(MeanMotionDt), double.Parse(MeanMotionDt2), double.Parse(BStar), 0, int.Parse(SetNumber));
                string line2 = TLE.GenerateLine2(int.Parse(NoradId), double.Parse(Inclination), double.Parse(RAAN), double.Parse(Eccentricity),
                    double.Parse(ArgPerigee), double.Parse(MeanAnomaly), double.Parse(MeanMotion), int.Parse(RevAtEpoch));

                TLE tleData = new TLE(new string[] { sat, line1, line2 });
                TLEString = tleData.ToString();
                IsChanged = true;
            }
            catch (SatellitesTleException ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message, TranslationSource.Instance["sError"]
                    , System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message, TranslationSource.Instance["sError"]
                    , System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error));
            }
        }
        private void OnSave(AdonisWindow? window)
        {
            try
            {
                TLE tle = new TLE(TLEString);
                OnTLE2Value();

                TLEData = tle;
                IsChanged = false;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message, TranslationSource.Instance["sError"]
                    , System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error));
                return;
            }
        }

        private void OnClose(AdonisWindow? window)
        {
            if (IsChanged)
            {
                var result = MessageBox.Show(TranslationSource.Instance["zCloseAnyway"], TranslationSource.Instance["sUnsavedChanges"],
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.No)
                {
                    return;
                }
            }
            window?.Close();
        }
    }
}
