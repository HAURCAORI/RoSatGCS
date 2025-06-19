using AdonisUI.Controls;
using CommunityToolkit.Mvvm.Input;
using GMap.NET.MapProviders;
using MessagePack;
using Newtonsoft.Json.Linq;
using NLog;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Utils.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class WindowSettingsViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private SettingsModel settingsModel;
        private static readonly string PathCacheSetting = "Cache/setting";

        private bool ByteArrayConversion(string value, out byte[] result)
        {
            value = value.Replace("0x", "").Replace("0X", "").Replace(" ", "").Replace("-", "");

            // Validate even number of characters
            if (value.Length % 2 != 0)
            {
                result = [];
                return false;
            }

            // Convert to byte array
            byte[] bytes = new byte[value.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            }
            result = bytes;
            return true;
        }
        private bool ByteConversion(string value, out byte result)
        {
            value = value.ToLower().Trim();
            if (value.StartsWith("0x") && value.Length == 4)
            {
                if (byte.TryParse(value.Replace("0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier, null, out byte ret))
                {
                    result = ret;
                    return true;
                }
            }
            else if (byte.TryParse(value, out byte ret))
            {
                result = ret;
                return true;
            }
            result = 0;
            return false;
        }

        private bool FrequencyConversion(string value, out long result)
        {
            value = value.ToLower().Trim();
            if(value.EndsWith("hz"))
            {
                if(value.EndsWith("ghz"))
                {
                    var val = value[..^3];
                    if(double.TryParse(val, out double d))
                    {
                        result = (long) (d * 1000000000.0);
                        return true;
                    }
                }
                else if (value.EndsWith("mhz"))
                {
                    var val = value[..^3];
                    if (double.TryParse(val, out double d))
                    {
                        result = (long) (d * 1000000.0);
                        return true;
                    }
                }
                else if (value.EndsWith("khz"))
                {
                    var val = value[..^3];
                    if (double.TryParse(val, out double d))
                    {
                        result = (long) (d * 1000.0);
                        return true;
                    }
                }
                else if (value.EndsWith("hz"))
                {
                    var val = value[..^2];
                    if (double.TryParse(val, out double d))
                    {
                        result = (long) d;
                        return true;
                    }
                }
                result = 0;
                return false;
            }
            else
            {
                if(long.TryParse(value, out long ret))
                {
                    result = ret;
                    return true;
                }
            }
            result = 0;
            return false;
        }

        private bool IPConversion(string value, out string result)
        {
            Match match = Regex.Match(value, @"^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$");
            if (match.Success)
            {
                result = value;
                return true;
            }

            result = "";
            return false;
        }

        private bool NumberConversion(string value, out int result)
        {
            if (int.TryParse(value, out int ret))
            {
                result = ret;
                return true;
            }
            result = 0;
            return false;
        }

        // Radio General Configuration
        private string _webSocketIPAddress = SettingsModel.Instance.WebSocketIPAddress;
        private string _webSocketPort = SettingsModel.Instance.WebSocketPort.ToString();
        private bool _webSocketTLS = SettingsModel.Instance.WebSocketTLS;

        public string WebSocketIPAddress { get => _webSocketIPAddress; set => _webSocketIPAddress = value; }
        public string WebSocketPort { get => _webSocketPort; set => _webSocketPort = value; }
        public bool WebSocketTLS { get => _webSocketTLS; set => _webSocketTLS = value; }


        // Radio General Radio Configuration
        private string _uplinkFreq = SettingsModel.Instance.UplinkFreq.ToString();
        private string _downlinkFreq = SettingsModel.Instance.DownlinkFreq.ToString();
        private string _radioMac = SettingsModel.Instance.RadioMac.ToString();
        private string _rfConfig = SettingsModel.Instance.RFConfig.ToString();

        public string UplinkFreq { get => _uplinkFreq; set => _uplinkFreq = value; }
        public string DownlinkFreq { get => _downlinkFreq; set => _downlinkFreq = value; }
        public string RadioMac { get => _radioMac; set => _radioMac = value; }
        public string RFConfig { get => _rfConfig; set => _rfConfig = value; }

        // Radio AES Configuration
        private string _aesIv = "";
        private string _aesKey = "";
        private bool _isEncrypted = SettingsModel.Instance.IsEncrypted;
        private bool _isBase64 = true;
        public string AesIv { get => _aesIv; set =>  SetProperty(ref _aesIv, value); }
        public string AesKey { get => _aesKey; set => SetProperty(ref _aesKey, value); }
        public bool IsEncrypted { get => _isEncrypted; set => _isEncrypted = value; }
        public bool IsBase64 { get => _isBase64; set { _isBase64 = value; OnBaseChanged(); } }

        // Processor Advanced Configuration
        private bool _debug = false;
        public bool IsDebug { get => _debug; set => _debug = value; }


        public ICommand ChangeLanguage { get; }
        public ICommand ClickOk { get; }
        public ICommand ClickCancel { get; }
        public ICommand ClickApply { get; }
        public ICommand PushProcessorGeneral { get; }
        public ICommand PushRadioConfiguration { get; }
        public ICommand PushEncryption { get; }
        public ICommand PushAdvanced { get; }

        public WindowSettingsViewModel()
        {
            settingsModel = SettingsModel.Instance;
            
            ChangeLanguage = new RelayCommand(OnChangeLanguage);
            ClickOk = new RelayCommand<AdonisWindow>(OnClickOk);
            ClickCancel = new RelayCommand<AdonisWindow>(OnClickCancel);
            ClickApply = new RelayCommand<AdonisWindow>(OnClickApply);
            PushProcessorGeneral = new RelayCommand(OnApplyProcessorGeneral);
            PushRadioConfiguration = new RelayCommand(OnPushRadioConfiguration);
            PushEncryption = new RelayCommand(OnPushEncryption);
            PushAdvanced = new RelayCommand(OnPushAdvanced);

            if (IsBase64)
            {
                if(settingsModel.AesIv != null)
                    _aesIv = Convert.ToBase64String(settingsModel.AesIv);
                if (settingsModel.AesKey != null)
                    _aesKey = Convert.ToBase64String(settingsModel.AesKey);
            }
            else
            {
                if (settingsModel.AesIv != null)
                    _aesIv = BitConverter.ToString(settingsModel.AesIv).Replace("-", "");
                if (settingsModel.AesKey != null)
                    _aesKey = BitConverter.ToString(settingsModel.AesKey).Replace("-", "");
            }
        }


        private void OnChangeLanguage()
        {
            switch(settingsModel.Language)
            {
                case SettingsModel.LanguageOption.Korean:
                    TranslationSource.SetLanguage("ko-KR");
                    break;
                case SettingsModel.LanguageOption.English:
                    TranslationSource.SetLanguage("en-US");
                    break;
                default:
                    break;
            }
            settingsModel.UpdateList();
        }

        private void OnClickOk(AdonisWindow? window)
        {
            ClickApply.Execute(window);
            if (window == null) return;
            window.Close();
        }

        private void OnClickCancel(AdonisWindow? window)
        {
            if (window == null) return;
            window.Close();
        }

        private void OnClickApply(AdonisWindow? window)
        {
            try
            {
                ApplyProcessorGeneral();
                ApplyRadioGeneral();
                ApplyRadioAES();

                Directory.CreateDirectory("Cache");
                using var fileStream = new FileStream(PathCacheSetting, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                MessagePackSerializer.Serialize(fileStream, settingsModel);
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(e.Message));
            }
        }

        private async void OnApplyProcessorGeneral()
        {
            try
            {
                ApplyProcessorGeneral();

                var packetProc = new ProcessorConfigPacket();
                packetProc.IP = settingsModel.WebSocketIPAddress;
                packetProc.Port = settingsModel.WebSocketPort;
                packetProc.TLS = settingsModel.WebSocketTLS;
                var retProc = await ZeroMqQueryExecutor.Instance.ExecuteAsync(packetProc, DispatcherType.NoResponse);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Application.Current.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(e.Message));
            }
        }

        private async void OnPushRadioConfiguration()
        {
            try
            {
                ApplyRadioGeneral();

                var packetConn = new CommandRadioPacket();
                packetConn.RemoteRadioMac = settingsModel.RadioMac;
                var retConn = await ZeroMqQueryExecutor.Instance.ExecuteAsync(packetConn, DispatcherType.NoResponse);

                
                var packetRadio = new CommandRadioPacket();
                packetRadio.DownlinkFrequency = (uint)settingsModel.DownlinkFreq;
                packetRadio.UplinkFrequency = (uint)settingsModel.UplinkFreq;
                packetRadio.RFConfig = settingsModel.RFConfig;
                var retRadio = await ZeroMqQueryExecutor.Instance.ExecuteAsync(packetRadio, DispatcherType.NoResponse);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Application.Current.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(e.Message));
            }
        }

        private async void OnPushEncryption()
        {
            try
            {
                ApplyRadioAES();

                var packetConn = new CommandRadioPacket();
                packetConn.AES.IV = settingsModel.AesIv;
                packetConn.AES.Key = settingsModel.AesKey;
                packetConn.Encrypted = settingsModel.IsEncrypted;
                var retConn = await ZeroMqQueryExecutor.Instance.ExecuteAsync(packetConn, DispatcherType.NoResponse);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Application.Current.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(e.Message));
            }
        }
        private async void OnPushAdvanced()
        {
            try
            {
                var packetDebug = new ProcessorDebugPacket();
                packetDebug.Debug = IsDebug;
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(packetDebug, DispatcherType.NoResponse);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Application.Current.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(e.Message));
            }
        }

        private void ApplyProcessorGeneral()
        {
            if (IPConversion(_webSocketIPAddress, out string ret0))
            {
                settingsModel.WebSocketIPAddress = ret0;
            }
            if (NumberConversion(_webSocketPort, out int ret1))
            {
                settingsModel.WebSocketPort = ret1;
            }
            settingsModel.WebSocketTLS = _webSocketTLS;
        }

        private void ApplyRadioGeneral()
        {
            if (FrequencyConversion(_uplinkFreq, out long ret0))
            {
                settingsModel.UplinkFreq = ret0;
            }
            if (FrequencyConversion(_downlinkFreq, out long ret1))
            {
                settingsModel.DownlinkFreq = ret1;
            }
            if (ByteConversion(_radioMac, out byte ret2))
            {
                settingsModel.RadioMac = ret2;
            }
            if (ByteConversion(_rfConfig, out byte ret3))
            {
                settingsModel.RFConfig = ret3;
            }
        }

        private void ApplyRadioAES()
        {
            settingsModel.IsEncrypted = _isEncrypted;
            if (IsBase64)
            {
                var iv = Convert.FromBase64String(AesIv);

                if(iv.Length == 16)
                {
                    settingsModel.AesIv = iv;
                }
                else
                {
                    throw new ArgumentException("AES IV must be 16 bytes");
                }

                var key = Convert.FromBase64String(AesKey);
                if (key.Length == 32)
                {
                    settingsModel.AesKey = key;
                }
                else
                {
                    throw new ArgumentException("AES Key must be 32 bytes");
                }
            }
            else
            {
                if(ByteArrayConversion(AesIv, out byte[] iv) && iv.Length == 16)
                {
                    settingsModel.AesIv = iv;
                }
                else
                {
                    throw new ArgumentException("AES IV must be 16 bytes");
                }
                if(ByteArrayConversion(AesKey, out byte[] key) && key.Length == 32)
                {
                    settingsModel.AesKey = key;
                }
                else
                {
                    throw new ArgumentException("AES Key must be 32 bytes");
                }
            }
        }

        private void OnBaseChanged()
        {
            if (IsBase64) {
                if (ByteArrayConversion(AesIv, out byte[] iv))
                {
                    AesIv = Convert.ToBase64String(iv);
                }
                if (ByteArrayConversion(AesKey, out byte[] key))
                {
                    AesKey = Convert.ToBase64String(key);
                }
            }
            else
            {
                AesIv = BitConverter.ToString(Convert.FromBase64String(AesIv)).Replace("-", "");
                AesKey = BitConverter.ToString(Convert.FromBase64String(AesKey)).Replace("-", "");
            }

        }


        public SettingsModel SettingsModel
        {
            get { return settingsModel; }
        }
    }
}
