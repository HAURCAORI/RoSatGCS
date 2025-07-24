using Newtonsoft.Json.Linq;
using NLog;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RoSatGCS.Utils.Query
{
    public abstract class QueryExecutorBase : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _disposed = false;
        public static List<byte> Serializer(List<List<object>> list)
        {
            List<byte> bytes = [];
            foreach (var element in list)
            {
                foreach(var value in element)
                {
                    if (value is byte b)
                        bytes.Add(b);
                    else if (value is sbyte sb)
                        bytes.Add((byte)sb);
                    else if (value is UInt16 s)
                        bytes.AddRange(BitConverter.GetBytes(s));
                    else if (value is Int16 us)
                        bytes.AddRange(BitConverter.GetBytes(us));
                    else if (value is UInt32 i)
                        bytes.AddRange(BitConverter.GetBytes(i));
                    else if (value is Int32 ui)
                        bytes.AddRange(BitConverter.GetBytes(ui));
                    else if (value is UInt64 l)
                        bytes.AddRange(BitConverter.GetBytes(l));
                    else if (value is Int64 ul)
                        bytes.AddRange(BitConverter.GetBytes(ul));
                    else if (value is float f)
                        bytes.AddRange(BitConverter.GetBytes(f));
                    else if (value is double d)
                        bytes.AddRange(BitConverter.GetBytes(d));
                    else if (value is string st)
                    {
                        bytes.AddRange(Encoding.Default.GetBytes(st));
                    }
                    else
                        throw new NotSupportedException($"Type is not supported.");
                }
            }
            return bytes;
        }

        public async Task<object?> ExecuteAsync(object data, DispatcherType dispatcher = DispatcherType.NoResponse)
        {
            var packet = CreateQueryPacket(data, dispatcher);
            if (packet == null)
            {
                Logger.Error("Invalid packet.");
                throw new ArgumentException("Invalid packet.");
            }
            var ret = await ExecuteQueryAsync(packet);
            if (ret == null) return null;

            return ParseResult(data, ret);
        }

        protected abstract Task<QueryPacket?> ExecuteQueryAsync(QueryPacket packet);

        protected static object? ParseResult(object data, QueryPacket packet)
        {

            if (data is SatelliteCommandModel c)
            {

                if (packet.Type == QueryType.Error)
                {
                    var err = CommandStatePacket.DeserializePacket(packet.Payload);
                    if (err == null) return null;
                    return err.Error;
                }

                var ret = CommandCpResultPacket.DeserializePacket(packet.Payload);
                if (ret == null) return null;
                return ret.Payload;
            }
            else
            {
                return null;
            }
        }

        protected static QueryPacket? CreateQueryPacket(object data, DispatcherType dispatcher)
        {
            QueryPacket? packet = null;
            if (data is SatelliteCommandModel c)
            {
                packet = new QueryPacket
                {
                    Id = Guid.NewGuid(),
                    Name = c.Name,
                    Type = QueryType.Command,
                    DispatcherType = dispatcher
                };

                Random rnd = new Random();

                var commandPacket = new CommandCpPacket
                {
                    ModuleMac = c.Module,
                    Gateway = c.Gateway,
                    CommandId = (ulong)(c.Id + rnd.Next(0, 5000)),
                    SatelliteId = "2",
                    NoProgressTimeout = "5s",
                    ReadTimeout = "999s",
                    WriteTimeout = "999s",
                    Payload = [.. c.InputSerialized]
                };

                AssignPayloadByGateway(commandPacket, c);

                string hex = string.Join(" ", commandPacket.Payload.Select(b => b.ToString("X2")));

                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Data: " + hex, "Hex Data"));

                packet.Payload = CommandCpPacket.SerializePacket(commandPacket);
            }
            else if (data is CommandRadioPacket cr)
            {
                packet = new QueryPacket
                {
                    Id = Guid.NewGuid(),
                    Name = "Radio",
                    Type = QueryType.Radio,
                    DispatcherType = dispatcher
                };
                packet.Payload = CommandRadioPacket.SerializePacket(cr);
            }
            else if (data is ProcessorConfigPacket cp)
            {
                packet = new QueryPacket
                {
                    Id = Guid.NewGuid(),
                    Name = "ProcessorConfig",
                    Type = QueryType.Config,
                    DispatcherType = dispatcher
                };

                packet.Payload = ProcessorConfigPacket.SerializePacket(cp);
            }
            else if (data is ProcessorDebugPacket dp)
            {
                packet = new QueryPacket
                {
                    Id = Guid.NewGuid(),
                    Name = "ProcessorDebug",
                    Type = QueryType.Debug,
                    DispatcherType = dispatcher
                };
                packet.Payload = ProcessorDebugPacket.SerializePacket(dp);
            }
            else if (data is FirmwareUpdatePacket fwupd)
            {
                packet = new QueryPacket
                {
                    Id = Guid.NewGuid(),
                    Name = "FirmwareUpdate",
                    Type = QueryType.FwUpdate,
                    DispatcherType = dispatcher
                };

                fwupd.CommandId = (ulong)(new Random().Next(0, 5000));

                packet.Payload = FirmwareUpdatePacket.SerializePacket(fwupd);
            }

            return packet;
        }

        private static void AssignPayloadByGateway(CommandCpPacket commandPacket, SatelliteCommandModel c)
        {
            var gateway = commandPacket.Gateway;
            switch(gateway)
            {
                case 1200:
                    if (commandPacket.Payload.Length > 5)
                    {
                        var size = commandPacket.Payload[4];
                        if (size == 0)
                        {
                            commandPacket.Payload = commandPacket.Payload[..5];
                        }
                        else if (size > commandPacket.Payload.Length - 5)
                        {
                            // Fill the payload to the expected size 
                            commandPacket.Payload = commandPacket.Payload
                                .Concat(new byte[size - (commandPacket.Payload.Length - 5)])
                                .ToArray();
                        }
                    }
                    
                    break;
                case 1202:
                    break;
                case 1212:
                case 1300:
                case 1302:
                    // MacFPCommand
                    // DataLen[UInt8] , FIDLID[Uint16], FunctionId[UInt32], Seq[UInt16], Error[UInt8], Payload[UInt8[]]
                    commandPacket.Payload = BitConverter.GetBytes((ushort)c.FIDLId)
                        .Concat(BitConverter.GetBytes((uint)c.Id))
                        .Concat(new byte[] { 0, 0, 0 })
                        .Concat(commandPacket.Payload)
                        .ToArray();
                    commandPacket.Payload = new byte[] { (byte)commandPacket.Payload.Length }.Concat(commandPacket.Payload).ToArray();
                    break;
                case 1450:
                    // Offset[UInt32], Payload[UInt8[]]
                    commandPacket.Payload = new byte[] { 0, 0, 0, 0 }.Concat(commandPacket.Payload).ToArray();
                    break;
                default:
                    break;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                }

                _disposed = true;
               
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
