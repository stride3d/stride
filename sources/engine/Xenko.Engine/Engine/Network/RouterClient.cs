// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Diagnostics;

namespace Xenko.Engine.Network
{
    public class RouterClient
    {
        public static readonly Logger Log = GlobalLogger.GetLogger("RouterClient");

        /// <summary>
        /// The default port to connect to router server.
        /// </summary>
        public static readonly int DefaultPort = 31254;

        /// <summary>
        /// The default port to listen for connection from router.
        /// </summary>
        public static readonly int DefaultListenPort = 31255;

        /// <summary>
        /// Starts a service.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void RegisterService()
        {
            // It will need the control connection (if not started yet)
            // Control connection will be able to list this service as available, and start an instance of it when requested
            throw new NotImplementedException();
        }

        /// <summary>
        /// Requests a specific server.
        /// </summary>
        /// <returns></returns>
        public static async Task<SimpleSocket> RequestServer(string serverUrl, CancellationToken cancellationToken = default(CancellationToken))
        {
            var socketContext = await InitiateConnectionToRouter().ConfigureAwait(false);

            using (cancellationToken.Register(() => socketContext.Dispose()))
            {
                await socketContext.WriteStream.WriteInt16Async((short)ClientRouterMessage.RequestServer).ConfigureAwait(false);
                await socketContext.WriteStream.WriteStringAsync(serverUrl).ConfigureAwait(false);
                await socketContext.WriteStream.FlushAsync().ConfigureAwait(false);

                var result = (ClientRouterMessage)await socketContext.ReadStream.ReadInt16Async().ConfigureAwait(false);
                if (result != ClientRouterMessage.ServerStarted)
                {
                    throw new SimpleSocketException("Could not connect to server");
                }

                var errorCode = await socketContext.ReadStream.ReadInt32Async().ConfigureAwait(false);
                if (errorCode != 0)
                {
                    var errorMessage = await socketContext.ReadStream.ReadStringAsync().ConfigureAwait(false);
                    throw new SimpleSocketException(errorMessage);
                }
            }

            return socketContext;
        }

        /// <summary>
        /// Initiates a connection to the router.
        /// </summary>
        /// <returns></returns>
        private static async Task<SimpleSocket> InitiateConnectionToRouter()
        {
            // Let's make sure this run in a different thread (in case some operation are blocking)
            var socketContextTCS = new TaskCompletionSource<SimpleSocket>();
            var socketContext = new SimpleSocket();
            socketContext.Connected = context =>
            {
                socketContextTCS.TrySetResult(context);
            };

            try
            {
                var serverAddress = Environment.GetEnvironmentVariable("XenkoConnectionRouterRemoteIP") ?? "127.0.0.1";

                // If connecting as a client, try once, otherwise try to listen multiple time (in case port is shared)
                switch (ConnectionMode)
                {
                    case RouterConnectionMode.Connect:
                        socketContext.StartClient(serverAddress, DefaultPort);
                        break;
                    case RouterConnectionMode.Listen:
                        socketContext.StartServer(DefaultListenPort, true, 10);
                        break;
                    case RouterConnectionMode.ConnectThenListen:
                        bool clientException = false;
                        try
                        {
                            socketContext.StartClient(serverAddress, DefaultPort);
                        }
                        catch (Exception) // Ideally we should filter SocketException, but not available on some platforms (maybe it should be wrapped in a type available on all paltforms?)
                        {
                            clientException = true;
                        }
                        if (clientException)
                        {
                            socketContext.StartServer(DefaultListenPort, true, 10);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Connection should happen within 10 seconds, otherwise consider there is no connection router trying to connect back to us
                if (await Task.WhenAny(socketContextTCS.Task, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false) != socketContextTCS.Task)
                {
                    throw new SimpleSocketException("Connection router did not connect back to our listen socket");
                }

                return await socketContextTCS.Task;
            }
            catch (Exception e)
            {
                Log.Error($"Could not connect to connection router using mode {ConnectionMode}", e);
                throw;
            }
        }

        private static void StartControlConnection()
        {
            // Start control connection
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether this platform initiates connections by listening on a port and wait for router (true) or connecting to router (false).
        /// </summary>
        private static RouterConnectionMode ConnectionMode
        {
            get
            {
                switch (Platform.Type)
                {
                    case PlatformType.UWP:
                        return RouterConnectionMode.ConnectThenListen;
                    case PlatformType.Android:
                    case PlatformType.iOS:
                        return RouterConnectionMode.Listen;
                    default:
                        return RouterConnectionMode.Connect;
                }
            }
        }

        private enum RouterConnectionMode
        {
            /// <summary>
            /// Tries to connect to the router.
            /// </summary>
            Connect = 1,

            /// <summary>
            /// Tries to listen from a router connection.
            /// </summary>
            Listen = 2,

            /// <summary>
            /// First, tries to connect, and if not possible, listen for a router connection.
            /// This is useful for platform where we can't be sure (no way to determine if emulator and/or run in desktop or remotely, such as UWP).
            /// </summary>
            ConnectThenListen = 3,
        }
    }
}
