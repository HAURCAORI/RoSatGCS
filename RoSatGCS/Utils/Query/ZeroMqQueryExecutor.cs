using GMap.NET.MapProviders;
using MessagePack;
using MessagePack.Resolvers;
using NetMQ;
using NetMQ.Sockets;
using RoSatGCS.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RoSatGCS.Utils.Query
{
    public sealed class ZeroMqQueryExecutor : QueryExecutorBase, IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Lazy<ZeroMqQueryExecutor> _instance = new(() => new ZeroMqQueryExecutor());

        public static ZeroMqQueryExecutor Instance => _instance.Value;

        private static readonly string _sendAddress = "tcp://127.0.0.1:50000"; // Send socket (Client)
        private static readonly string _receiveAddress = "tcp://127.0.0.1:50001"; // Receive socket (Server)
        private static readonly int _timeout = 5; // seconds
        private static readonly int _postpone = 10; // seconds
        private static readonly int _fileTimeout = 600; // seconds for file transfer

        private ThreadLocal<RequestSocket?> _sendSocket;
        private ResponseSocket? _receiveSocket;
        private readonly object _lock = new();
        private bool _disposed = false;
        private CancellationTokenSource? _cts;

        private readonly Mutex _mutex = new();

        // Stores pending command tasks waiting for execution results
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> _pendingResults = new();

        private ZeroMqQueryExecutor()
        {
            _sendSocket = new ThreadLocal<RequestSocket?>(() => CreateRequestSocket());
            StartListening();
        }

        public void Init()
        {
            lock (_lock)
            {
                ReinitializeSockets();
            }
        }
        
        protected override async Task<QueryPacket?> ExecuteQueryAsync(QueryPacket packet)
        {
            if (_disposed) {
                Logger.Error("ZeroMqCommandExecutor is disposed.");
                throw new ObjectDisposedException(nameof(ZeroMqQueryExecutor));
            }

            var tcs = new TaskCompletionSource<object>();
            if (packet.DispatcherType == DispatcherType.Postpone || packet.DispatcherType == DispatcherType.FileTransfer)
            {
                _pendingResults[packet.Id] = tcs; // Store result only if we expect a response later
            }

            return await Task.Run(() =>
            {
                RequestSocket? socket = null;

                if (_sendSocket.Value == null)
                {
                    _sendSocket.Value = CreateRequestSocket();
                }
                socket = _sendSocket.Value;

                if (socket == null)
                {
                    Logger.Error("Send socket is not initialized.");
                    throw new System.Exception("Send socket is not initialized.");
                }

                lock (socket) // Thread-safe sending
                {
                    socket.SendFrame(QueryPacket.SerializePacket(packet));

                    int timeout = _timeout;
                    if(packet.Type == QueryType.Command)
                    {
                        timeout = 5;
                    }
                    if (socket.TryReceiveFrameBytes(TimeSpan.FromSeconds(timeout), out byte[]? respond))
                    {
                        // Success to receive response
                        if (packet.DispatcherType == DispatcherType.NoResponse) { return null; }

                        var ret = QueryPacket.DeserializePacket(respond);

                        if (packet.DispatcherType == DispatcherType.Postpone)
                        {
                            if(tcs.Task.Wait(TimeSpan.FromSeconds(_postpone)))
                            {
                                return tcs.Task.Result as QueryPacket;
                            }
                            else
                            {
                                Logger.Error("Timeout waiting for execution result.");
                                throw new TimeoutException("Timeout waiting for execution result.");
                            }
                        }
                        else if (packet.DispatcherType == DispatcherType.FileTransfer)
                        {
                            if(tcs.Task.Wait(TimeSpan.FromSeconds(_fileTimeout)))
                            {
                                return tcs.Task.Result as QueryPacket;
                            }
                            else
                            {
                                Logger.Error("Timeout waiting for file transfer result.");
                                throw new TimeoutException("Timeout waiting for file transfer result.");
                            }
                        }
                        else if (packet.DispatcherType == DispatcherType.ImmediateResponse)
                        {
                            return ret;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        // Fail to receive response
                        if (_sendSocket.Value != null)
                        {
                            // Socket reset
                            _sendSocket.Value.Dispose();
                            _sendSocket.Value = null;
                        }

                        Logger.Error("No ACK received from server.");
                        throw new TimeoutException("No ACK received from server.");
                        
                    }
                }
            });
        }

        public override async Task CancelAllQueryAsync()
        {
            _mutex.WaitOne();
            foreach (var tcs in _pendingResults.Values)
            {
                tcs.SetCanceled();
            }
            _mutex.ReleaseMutex();
            _pendingResults.Clear();

            await ExecuteAsync(new CancelPacket(), DispatcherType.NoResponse);
        }

        /// <summary>
        /// Starts the receiving server on a background thread.
        /// </summary>
        private void StartListening()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                lock (_lock)
                {
                    _receiveSocket = new ResponseSocket();
                    _receiveSocket.Connect(_receiveAddress);

                    while (!_disposed && !_cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            byte[] receivedMessage = _receiveSocket.ReceiveFrameBytes();
                            var result = QueryPacket.DeserializePacket(receivedMessage);

                            _mutex.WaitOne();

                            if (result is { Type: QueryType.Data })
                            {
                                var data = BeaconDataPacket.DeserializePacket(result.Payload);
                                if (data != null && data.Timestamps.Length == data.Count && data.Data.Length == data.Count * DataTypeInfo.SizeOf(data.Type))
                                {
                                    int elemSize = DataTypeInfo.SizeOf(data.Type);

                                    var list = new List<PlotData>(data.Count);
                                    var src = data.Data.AsSpan();

                                    for (int i = 0; i < data.Count; i++)
                                    {
                                        var slice = src.Slice(i * elemSize, elemSize).ToArray();

                                        var ts = data.Timestamps[i];
                                        long subTicks = (long)ts.SubTicks * 1000L; // 0.1 ms = 100 μs = 1,000 ticks
                                        var dtUtc = DateTimeOffset.FromUnixTimeSeconds(ts.Unix).AddTicks(subTicks).UtcDateTime;

                                        var pd = new PlotData
                                        {
                                            DateTime = dtUtc,
                                            PlotDataType = data.Type,
                                            Data = slice
                                        };

                                        list.Add(pd);
                                    }

                                    MainDataContext.Instance?.PlotDataContainer.Add(data.DataID, CollectionsMarshal.AsSpan(list));
                                }
                            }
                            else if (result is { Type: QueryType.Info })
                            {
                                var info = InfoPacket.DeserializePacket(result.Payload);
                                if (info != null)
                                {
                                    switch (info.Type)
                                    {
                                        case InfoType.None:
                                            RoSatGCS.Utils.Logger.Logger.LogInfo(info.Info);
                                            break;
                                        case InfoType.Send:
                                            RoSatGCS.Utils.Logger.Logger.LogInfo(info.Info);
                                            break;
                                        case InfoType.Receive:
                                            RoSatGCS.Utils.Logger.Logger.LogInfo(info.Info);
                                            break;
                                        case InfoType.Status:
                                            RoSatGCS.Utils.Logger.Logger.LogInfo(info.Info);
                                            break;
                                        case InfoType.Warning:
                                            RoSatGCS.Utils.Logger.Logger.LogWarning(info.Info);
                                            break;
                                        case InfoType.Error:
                                            RoSatGCS.Utils.Logger.Logger.LogError(info.Info);
                                            break;
                                        default:
                                            RoSatGCS.Utils.Logger.Logger.LogInfo(info.Info);
                                            break;
                                    }
                                }
                            }
                            else if (result != null && _pendingResults.TryRemove(result.Id, out var tcs))
                            {
                                tcs.SetResult(result);
                            }

                            _mutex.ReleaseMutex();

                            _receiveSocket.SendFrame("ACK");
                        }
                        catch (System.Exception ex)
                        {
                            if (!_disposed)
                            {
                                Logger.Error($"Error receiving data: {ex.Message}");
                            }
                        }
                    }
                }
            }, _cts.Token);
        }

        private void ReinitializeSockets()
        {
            DisposeSockets();
            _sendSocket = new ThreadLocal<RequestSocket?>(() => CreateRequestSocket());
            StartListening();
        }

        private RequestSocket CreateRequestSocket()
        {
            var socket = new RequestSocket();
            socket.Connect(_sendAddress);
            return socket;
        }

        private void DisposeSockets()
        {
            foreach (var socket in _sendSocket.Values)
            {
                socket?.Dispose();
            }
            _sendSocket.Dispose();

            if (_receiveSocket != null)
            {
                _receiveSocket.Dispose();
                _receiveSocket = null;
            }

            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    lock (_lock)
                    {
                        DisposeSockets();
                    }
                }

                _disposed = true;
            }
        }

        public new void Dispose()
        {
            Dispose(true);
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
