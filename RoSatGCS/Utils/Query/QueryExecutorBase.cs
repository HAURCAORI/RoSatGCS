using Newtonsoft.Json.Linq;
using NLog;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
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
                        bytes.Add(0x00);
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

                var commandPacket = new CommandCpPacket
                {
                    ModuleMac = 0,
                    Gateway = 0,
                    CommandId = (ulong)c.Id,
                    SatelliteId = "",
                    NoProgressTimeout = "",
                    ReadTimeout = "",
                    WriteTimeout = "",
                    Payload = [.. QueryExecutorBase.Serializer(c.InputParameters)]
                };

                packet.Payload = CommandCpPacket.SerializePacket(commandPacket);
            }

            return packet;
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
