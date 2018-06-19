// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_UWP
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Sockets.Plugin.Abstractions;

// ReSharper disable once CheckNamespace

namespace Sockets.Plugin
{
    /// <summary>
    ///     Binds to a port and listens for TCP connections.
    ///     Use <code>StartListeningAsync</code> to bind to a local port, then handle <code>ConnectionReceived</code> events as
    ///     clients connect.
    /// </summary>
    class TcpSocketListener : ITcpSocketListener
    {
        private StreamSocketListener _backingStreamSocketListener;
        private CancellationTokenSource _listenCanceller;
        private readonly int _bufferSize;

        public TcpSocketListener()
        {
        }

        public TcpSocketListener(int bufferSize)
        {
            _bufferSize = bufferSize;
        }

        /// <summary>
        ///     Fired when a new TCP connection has been received.
        ///     Use the <code>SocketClient</code> property of the <code>TcpSocketListenerConnectEventArgs</code>
        ///     to get a <code>TcpSocketClient</code> representing the connection for sending and receiving data.
        /// </summary>
        public EventHandler<TcpSocketListenerConnectEventArgs> ConnectionReceived { get; set; }

        /// <summary>
        ///     Binds the <code>TcpSocketListener</code> to the specified port on all endpoints and listens for TCP connections.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="listenOn">The <code>CommsInterface</code> to listen on. If unspecified, all interfaces will be bound.</param>
        /// <param name="inheritHandle">Allows handle inheritance. Might be ignored depending on platform.</param>
        /// <returns></returns>
        public Task StartListeningAsync(int port, ICommsInterface listenOn = null, bool inheritHandle = false)
        {
            if (listenOn != null && !listenOn.IsUsable)
                throw new InvalidOperationException("Cannot listen on an unusable interface. Check the IsUsable property before attemping to bind.");
            
            _listenCanceller = new CancellationTokenSource();
            _backingStreamSocketListener = new StreamSocketListener();

            _backingStreamSocketListener.ConnectionReceived += (sender, args) =>
            {
                var nativeSocket = args.Socket;
                var wrappedSocket = new TcpSocketClient(nativeSocket, _bufferSize);

                var eventArgs = new TcpSocketListenerConnectEventArgs(wrappedSocket);
                if (ConnectionReceived != null)
                    ConnectionReceived(this, eventArgs);
            };

#if !WP80
            if (listenOn != null)
            {
                var adapter = ((CommsInterface) listenOn).NativeNetworkAdapter;

                return _backingStreamSocketListener.BindServiceNameAsync(port.ToString(), SocketProtectionLevel.PlainSocket, adapter).AsTask();
            }
            else
#endif
                return _backingStreamSocketListener.BindServiceNameAsync(port.ToString()).AsTask();
        }
        
        /// <summary>
        ///     Stops the <code>TcpSocketListener</code> from listening for new tcp connections.
        ///     This does not disconnect existing connections.
        /// </summary>
        public Task StopListeningAsync()
        {   
            return Task.Run(() =>
            {
                _listenCanceller.Cancel();
                _backingStreamSocketListener.Dispose();
                _backingStreamSocketListener = null;
            });
        }

        /// <summary>
        ///     The port to which the TcpSocketListener is currently bound
        /// </summary>
        public int LocalPort
        {
            get
            {
                return _backingStreamSocketListener != null
                    ? Int32.Parse(_backingStreamSocketListener.Information.LocalPort)
                    : 0;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
        /// </summary>
        ~TcpSocketListener()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_backingStreamSocketListener != null)
                    (_backingStreamSocketListener).Dispose();
            }
        }
    }
}
#endif
