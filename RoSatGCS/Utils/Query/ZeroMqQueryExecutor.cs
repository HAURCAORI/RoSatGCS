using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using System.Collections.Concurrent;
using MessagePack;
using System.ComponentModel.Design;
using System.Windows.Threading;
using System.Windows;
using MessagePack.Resolvers;
using GMap.NET.MapProviders;

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
        private static readonly int _postpone = 300; // seconds

        private ThreadLocal<RequestSocket?> _sendSocket;
        private ResponseSocket? _receiveSocket;
        private readonly object _lock = new();
        private bool _disposed = false;
        private CancellationTokenSource? _cts;

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
            if (packet.DispatcherType == DispatcherType.Postpone)
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
                        timeout = 120;
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

                            if (result != null && _pendingResults.TryRemove(result.Id, out var tcs))
                            {
                                tcs.SetResult(result);
                            }

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
