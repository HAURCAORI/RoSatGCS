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
        Postpone
    }

    public enum QueryType
    {
        None,
        ACK,
        Error,
        Command,
        Data,
        Schedule,
        Service
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
    public class CommandFwUpdatePacket : PacketBase<CommandFwUpdatePacket>
    {
        [Key(0)] public CommandCpPacket Command = new();
        [Key(1)] public CommandAesPacket AES = new();
        [Key(2)] public ushort BoardRevision;
        [Key(3)] public ushort CpuType;
        [Key(4)] public string FileName = "";
        [Key(5)] public ulong Flags;
        [Key(6)] public ushort FWType;
        [Key(7)] public ushort FWVersionMajor;
        [Key(8)] public ushort FWVersionMinor;
        [Key(9)] public ushort ModuleConfig;
        [Key(10)] public ushort ModuleType;
        [Key(11)] public ushort Submodule;
    }

    [MessagePackObject]
    public class CommandFwUpdateResultPacket : PacketBase<CommandFwUpdateResultPacket>
    {
        [Key(0)] public ulong CommandId;
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
}
