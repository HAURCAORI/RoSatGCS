using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;
using RoSatGCS.Utils.Localization;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static MessagePack.GeneratedMessagePackResolver.RoSatGCS.Models;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]

    public partial class SettingsModel : ObservableObject
    {

        private SettingsModel()
        {
            languageList = new List<LanguageOption>();
            foreach (LanguageOption val in Enum.GetValues(typeof(LanguageOption)))
            {
                languageList.Add(val);
            }
        }

        private static Lazy<SettingsModel> _instance = new(() => new SettingsModel());

        public static SettingsModel Instance { get { return _instance.Value; } }

        public static void Load(SettingsModel model)
        {
            _instance = new Lazy<SettingsModel>(() => model);
            switch (model.Language)
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
        }


        #region enums
        [Flags]
        public enum OptionChanged : ulong
        {
            General_Interface_Language = 1 << 0,
            General_Interface_Theme = 1 << 1,
        }

        public enum LanguageOption
        {
            Korean,
            English
        }
        #endregion

        #region fields
        private static OptionChanged optionChanged;

        [Key("ln")]
        private LanguageOption language;
        [Key("li")]
        private int languageIndex;
        [Key("ll")]
        private List<LanguageOption> languageList;

        #endregion

        #region properties
        [IgnoreMember]
        public LanguageOption Language
        {
            get { return language; }
            set {  SetProperty(ref language, value); Changed(OptionChanged.General_Interface_Language); }
        }

        [IgnoreMember]
        public int LanguageIndex
        {
            get { return languageIndex; }
            set { SetProperty(ref languageIndex, value);
                if (Enum.IsDefined(typeof(LanguageOption), value))
                    Language = (LanguageOption)value;
            }
        }

        [IgnoreMember]
        public List<LanguageOption> LanguageList { get { return languageList; } }

        // Radio
        [Key("ip")]
        private string _webSocketIPAddress;
        [IgnoreMember]
        public string WebSocketIPAddress { get => _webSocketIPAddress; set => SetProperty(ref _webSocketIPAddress, value); }
        [Key("pt")]
        private int _webSocketPort;
        [IgnoreMember]
        public int WebSocketPort { get => _webSocketPort; set => SetProperty(ref _webSocketPort, value); }
        [Key("tls")]
        private bool _webSocketTLS;
        [IgnoreMember]
        public bool WebSocketTLS { get => _webSocketTLS; set => SetProperty(ref _webSocketTLS, value); }


        [Key("uf")]
        private long _uplinkFreq;
        [Key("df")]
        private long _downlinkFreq;
        [Key("rm")]
        private byte _radioMac;
        [Key("rc")]
        private byte _rfConfig;
        [IgnoreMember]
        public long UplinkFreq { get => _uplinkFreq; set => SetProperty(ref _uplinkFreq, value); }
        [IgnoreMember]
        public long DownlinkFreq { get => _downlinkFreq; set => SetProperty(ref _downlinkFreq, value); }
        [IgnoreMember]
        public byte RadioMac { get => _radioMac; set => SetProperty(ref _radioMac, value); }
        [IgnoreMember]
        public byte RFConfig { get => _rfConfig; set => SetProperty(ref _rfConfig, value); }

        [Key("ai")]
        private byte[] _aesIv;
        [Key("ak")]
        private byte[] _aesKey;
        [Key("en")]
        private bool _encryption;
        [IgnoreMember]
        public byte[] AesIv { get => _aesIv; set => SetProperty(ref _aesIv, value); }
        [IgnoreMember]
        public byte[] AesKey { get => _aesKey; set => SetProperty(ref _aesKey, value); }
        [IgnoreMember]
        public bool IsEncrypted { get => _encryption; set => SetProperty(ref _encryption, value); }

        #endregion

        public static void Changed(OptionChanged op)
        {
            optionChanged |= op;
        }

        public void UpdateList()
        {
            var index = LanguageIndex;
            OnPropertyChanged(nameof(LanguageList));
            LanguageIndex = index;
        }

    }
}
