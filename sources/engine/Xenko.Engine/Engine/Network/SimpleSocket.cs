// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Threading.Tasks;
using Sockets.Plugin;

namespace Xenko.Engine.Network
{
    /// <summary>
    /// Manages socket connection+ack and low-level communication.
    /// High-level communication is supposed to happen in <see cref="SocketMessageLayer"/>.
    /// </summary>
    public class SimpleSocket : IDisposable
    {
        private const uint MagicAck = 0x35AABBCC;

        private TcpSocketClient socket;
        private bool isConnected;

        public Stream ReadStream
        {
            get { return socket.ReadStream; }
        }

        public Stream WriteStream
        {
            get { return socket.WriteStream; }
        }

        public string RemoteAddress
        {
            get { return socket.RemoteAddress; }
        }

        public int RemotePort
        {
            get { return socket.RemotePort; }
        }

        /// <summary>
        /// Gets the underlying <see cref="TcpSocketClient"/> object.
        /// </summary>
        internal TcpSocketClient Socket => socket;

        // Called on a succesfull connection
        public Action<SimpleSocket> Connected;

        // Called if there is a socket failure (after ack handshake)
        public Action<SimpleSocket> Disconnected;

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeSocket();
        }

        public async Task StartServer(int port, bool singleConnection, int retryCount = 1)
        {
            // Create TCP listener
            var listener = new TcpSocketListener(2048);

            listener.ConnectionReceived = async (sender, args) =>
            {
                var clientSocketContext = new SimpleSocket();

                try
                {
                    // Stop listening if we accept only a single connection
                    if (singleConnection)
                        await listener.StopListeningAsync().ConfigureAwait(false);

                    clientSocketContext.SetSocket((TcpSocketClient)args.SocketClient);

                    // Do an ack with magic packet (necessary so that we know it's not a dead connection,
                    // it sometimes happen when doing port forwarding because service don't refuse connection right away but only fails when sending data)
                    await SendAndReceiveAck(clientSocketContext.socket, MagicAck, MagicAck).ConfigureAwait(false);

                    Connected?.Invoke(clientSocketContext);

                    clientSocketContext.isConnected = true;
                }
                catch (Exception)
                {
                    clientSocketContext.DisposeSocket();
                }
            };

            for (int i = 0; i < retryCount; ++i)
            {
                try
                {
                    // Start listening
                    await listener.StartListeningAsync(port).ConfigureAwait(false);
                    break; // Break if no exception, otherwise retry
                }
                catch (Exception)
                {
                    // If there was an exception last try, propragate exception
                    if (i == retryCount - 1)
                        throw;
                }
            }
        }

        public async Task StartClient(string address, int port, bool needAck = true)
        {
            // Create TCP client
            var socket = new TcpSocketClient(2048);

            try
            {
                await socket.ConnectAsync(address, port).ConfigureAwait(false);

                SetSocket(socket);
                //socket.NoDelay = true;

                // Do an ack with magic packet (necessary so that we know it's not a dead connection,
                // it sometimes happen when doing port forwarding because service don't refuse connection right away but only fails when sending data)
                if (needAck)
                    await SendAndReceiveAck(socket, MagicAck, MagicAck).ConfigureAwait(false);

                Connected?.Invoke(this);

                isConnected = true;
            }
            catch (Exception)
            {
                DisposeSocket();
                throw;
            }
        }

        private static async Task SendAndReceiveAck(TcpSocketClient socket, uint sentAck, uint expectedAck)
        {
            await socket.WriteStream.WriteInt32Async((int)sentAck).ConfigureAwait(false);
            await socket.WriteStream.FlushAsync().ConfigureAwait(false);
            var ack = (uint)await socket.ReadStream.ReadInt32Async().ConfigureAwait(false);
            if (ack != expectedAck)
                throw new SimpleSocketException("Invalid ack");
        }

        private void SetSocket(TcpSocketClient socket)
        {
            this.socket = socket;
        }

        private void DisposeSocket()
        {
            if (this.socket != null)
            {
                if (isConnected)
                {
                    isConnected = false;
                    Disconnected?.Invoke(this);
                }

                this.socket.Dispose();
                this.socket = null;
            }
        }
    }
}
