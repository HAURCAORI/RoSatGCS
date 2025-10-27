using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace RoSatGCS.Utils.Query
{
    public enum DispatcherType
    {
        NoResponse,
        ImmediateResponse,
        Postpone,
        FileTransfer
    }

    public enum QueryType
    {
        None,
        ACK,
        Error,
        Command, // SpaceComms Command
        Radio,   // SpaceComms Radio
        Data,    // Beacon Data
        Schedule,
        Service,
        Config,  // SpaceComms Config
        Debug,   // Processor Debug Mode
        FwUpdate,// Firmware Update
        Cancel   // Cancel Command
    }

    public enum DataType
    {
        Int8,
        UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
        String,
        ByteArray,
        Boolean
    }

    public class PacketBase<T>
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static IFormatterResolver _resolver =  MessagePack.Resolvers.CompositeResolver.Create(NativeGuidResolver.Instance, StandardResolver.Instance);

        public static byte[] SerializePacket(T packet)
        {
            try
            {
                return MessagePackSerializer.Serialize(packet, MessagePackSerializerOptions.Standard.WithResolver(_resolver));
            }
            catch(System.Exception e)
            {
                Logger.Error($"Serialize Error:{e.Message}");
                return [];
            }
        }

        public static T? DeserializePacket(byte[] data)
        {
            try
            {
                return MessagePackSerializer.Deserialize<T>(data, MessagePackSerializerOptions.Standard.WithResolver(_resolver));
            }
            catch(System.Exception e)
            {
                Logger.Error($"Deserialize Error:{e.Message}");
                return default;
            }
        }
    }

    [MessagePackObject]
    public class QueryPacket : PacketBase<QueryPacket>
    {
        [Key(0)] public required Guid Id;
        [Key(1)] public required string Name;
        [Key(2)] public required QueryType Type;
        [Key(3)] public required DispatcherType DispatcherType;
        [Key(4)] public byte[] Payload = [];
    }

    [MessagePackObject]
    public class ErrorPacket : PacketBase<ErrorPacket>
    {
        [Key(0)] public UInt32 Code;
        [Key(1)] public string Error = "";
    }

    [MessagePackObject]
    public class ProcessorConfigPacket : PacketBase<ProcessorConfigPacket>
    {
        [Key(0)] public string IP = "";
        [Key(1)] public int Port = 0;
        [Key(2)] public bool TLS = false;
    }

    [MessagePackObject]
    public class ProcessorDebugPacket : PacketBase<ProcessorDebugPacket>
    {
        [Key(0)] public bool Debug = false;
    }

    [MessagePackObject]
    public class CancelPacket : PacketBase<CancelPacket>
    {
        [Key(0)] public ulong CommandId;
    }


    #region Command
    [MessagePackObject]
    public class CommandCpPacket : PacketBase<CommandCpPacket>
    {
        [Key(0)] public byte ModuleMac;
        [Key(1)] public ushort Gateway;
        [Key(2)] public ulong CommandId;
        [Key(3)] public string SatelliteId = "";
        [Key(4)] public string NoProgressTimeout = "";
        [Key(5)] public string ReadTimeout = "";
        [Key(6)] public string WriteTimeout = "";
        [Key(7)] public byte[] Payload = [];
    }

    [MessagePackObject]
    public class CommandCpResultPacket : PacketBase<CommandCpResultPacket>
    {
        [Key(0)] public ulong CommandId;
        [Key(1)] public byte[] Payload = [];
    }

    [MessagePackObject]
    public class CommandAesPacket : PacketBase<CommandAesPacket>
    {
        [Key(0)] public byte[] IV = new byte[16];
        [Key(1)] public byte[] Key = new byte[32];
    }

    [MessagePackObject]
    public class CommandRadioPacket : PacketBase<CommandRadioPacket>
    {
        [Key(0)] public CommandAesPacket AES = new();
        [Key(1)] public uint DownlinkFrequency;
        [Key(2)] public uint UplinkFrequency;
        [Key(3)] public bool Encrypted;
        [Key(4)] public uint RFConfig;
        [Key(5)] public byte RemoteRadioMac;
    }

    [MessagePackObject]
    public class CommandRadioResultPacket : PacketBase<CommandRadioResultPacket>
    {
        [Key(0)] public CommandAesPacket AES = new();
        [Key(1)] public uint DownlinkFrequency;
        [Key(2)] public uint UplinkFrequency;
        [Key(3)] public bool Encrypted;
        [Key(4)] public uint RFConfig;
        [Key(5)] public byte RemoteRadioMac;
    }

    [MessagePackObject]
    public class CommandRotatorPacket : PacketBase<CommandRotatorPacket>
    {
        [Key(0)] public uint Azimuth;
        [Key(1)] public uint Elevation;
    }

    [MessagePackObject]
    public class CommandRotatorResultPacket : PacketBase<CommandRotatorResultPacket>
    {
        [Key(0)] public uint Azimuth;
        [Key(1)] public uint Elevation;
    }


    [MessagePackObject]
    public class CommandBeaconPacket : PacketBase<CommandBeaconPacket>
    {
        [Key(0)] public string Duration = "";
        [Key(1)] public byte[] AX25Frame = [];
    }

    [MessagePackObject]
    public class CommandStatePacket : PacketBase<CommandStatePacket>
    {
        [Key(0)] public string Error = "";
        [Key(1)] public byte[] Data = [];
        [Key(2)] public string Info = "";
        [Key(3)] public string Notifier = "";
    }
    #endregion

    #region File and Firmware
    [MessagePackObject]
    public class FirmwareUpdatePacket : PacketBase<FirmwareUpdatePacket>
    {
        [Key(0)] public string FilePath = "";
        [Key(1)] public string SatelliteId = "";
        [Key(2)] public byte ModuleMac = 0;
        [Key(3)] public ushort BoardRevision = 0;
        [Key(4)] public ushort CpuType = 0;
        [Key(5)] public ushort SubModule = 0;
        [Key(6)] public ushort FWType = 0;
        [Key(7)] public ushort FWVerMaj = 0;
        [Key(8)] public ushort FWVerMin = 0;
        [Key(9)] public ushort ModuleType = 0;
        [Key(10)] public ushort ModuleConfig = 0;
        [Key(11)] public ulong Flags = 0;
        [Key(12)] public ulong CommandId = 0;
        [Key(13)] public bool IsBundle = false;
        [Key(14)] public bool IsFile = false;
    }

    [MessagePackObject]
    public class FirmwareUpdateResultPacket : PacketBase<FirmwareUpdateResultPacket>
    {
        [Key(0)] public ulong CommandId;
    }
    #endregion

    #region Beacon Data
    [MessagePackObject]
    public class BeaconTimestampPacket : PacketBase<BeaconTimestampPacket>
    {
        [Key(0)] public uint Unix;       // Unix time
        [Key(1)] public ushort SubTicks; // 0.1ms
    }

    [MessagePackObject]
    public class BeaconDataPacket : PacketBase<BeaconDataPacket>
    {
        [Key(0)] public ushort DataID = 0;
        [Key(1)] public DataType Type = DataType.UInt8;
        [Key(2)] public ushort Count = 1;
        [Key(3)] public BeaconTimestampPacket[] Timestamps = []; // Array of timestamps
        [Key(4)] public byte[] Data = []; // Raw data
    }
    #endregion

}
