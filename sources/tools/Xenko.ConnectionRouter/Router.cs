// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Assets;
using Xenko.Core.Diagnostics;
using Xenko.Engine.Network;

namespace Xenko.ConnectionRouter
{
    public class Router
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("Router");

        private Dictionary<string, TaskCompletionSource<Service>> registeredServices = new Dictionary<string, TaskCompletionSource<Service>>();
        private Dictionary<Guid, TaskCompletionSource<SimpleSocket>> pendingServers = new Dictionary<Guid, TaskCompletionSource<SimpleSocket>>();

        public async Task Listen(int port)
        {
            Log.Info($"Start to listen on port {port}");

            var socketContext = CreateSocketContext();
            await socketContext.StartServer(port, false);
        }

        /// <summary>
        /// Tries to connect. Blocks until connection fails or happens (if connection happens, it will launch the message loop in a separate unobserved Task).
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        public async Task TryConnect(string address, int port)
        {
            var socketContext = CreateSocketContext();

            // Wait for a connection to be possible on adb forwarded port
            await socketContext.StartClient(address, port).ConfigureAwait(false);
        }

        private SimpleSocket CreateSocketContext()
        {
            var socketContext = new SimpleSocket();
            socketContext.Connected = (clientSocketContext) =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // Routing
                        var routerMessage = (RouterMessage)await clientSocketContext.ReadStream.ReadInt16Async();

                        Log.Info($"Client {clientSocketContext.RemoteAddress}:{clientSocketContext.RemotePort} connected, with message {routerMessage}");

                        switch (routerMessage)
                        {
                            case RouterMessage.TaskProvideServer:
                            {
                                await HandleMessageServiceProvideServer(clientSocketContext, true);
                                break;
                            }
                            case RouterMessage.ServiceProvideServer:
                            {
                                await HandleMessageServiceProvideServer(clientSocketContext, false);
                                break;
                            }
                            case RouterMessage.ServerStarted:
                            {
                                await HandleMessageServerStarted(clientSocketContext);
                                break;
                            }
                            case RouterMessage.ClientRequestServer:
                            {
                                await HandleMessageClientRequestServer(clientSocketContext);
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException(string.Format("Router: Unknown message: {0}", routerMessage));
                        }
                    }
                    catch (Exception e)
                    {
                        // TODO: Ideally, separate socket-related error messages (disconnection) from real errors
                        // Unfortunately, it seems WinRT returns Exception, so it seems we can't filter with SocketException/IOException only?
                        Log.Info($"Client {clientSocketContext.RemoteAddress}:{clientSocketContext.RemotePort} disconnected with exception.", e);
                        clientSocketContext.Dispose();
                    }
                });
            };

            return socketContext;
        }

        /// <summary>
        /// Handles ClientRequestServer messages.
        /// It will try to find a matching service (spawn it if not started yet), and ask it to establish a new "server" connection back to us.
        /// </summary>
        /// <param name="clientSocket">The client socket context.</param>
        /// <returns></returns>
        private async Task HandleMessageClientRequestServer(SimpleSocket clientSocket)
        {
            // Check for an existing server
            // TODO: Proper Url parsing (query string)
            var url = await clientSocket.ReadStream.ReadStringAsync();

            Log.Info($"Client {clientSocket.RemoteAddress}:{clientSocket.RemotePort} sent message ClientRequestServer with URL {url}");

            string[] urlSegments;
            string urlParameters;
            RouterHelper.ParseUrl(url, out urlSegments, out urlParameters);
            if (urlSegments.Length == 0)
                throw new InvalidOperationException("No URL Segments");

            SimpleSocket serverSocket = null;
            ExceptionDispatchInfo serverSocketCapturedException = null;

            try
            {
                // For now, we handle only "service" URL
                switch (urlSegments[0])
                {
                    case "service":
                    {
                        // From the URL, start service (if not started yet) and ask it to provide a server
                        serverSocket = await SpawnServerFromService(url, false);
                        break;
                    }
                    case "task":
                    {
                        // From the URL, start service (if not started yet) and ask it to provide a server
                        serverSocket = await SpawnServerFromService(url, true);
                        break;
                    }
                    case "redirect":
                    {
                        // Redirect to a IP/port
                        serverSocket = new SimpleSocket();
                        var host = urlSegments[1];
                        var port = int.Parse(urlSegments[2]);

                        // Note: for security reasons, we currently use a whitelist
                        //if (host == "xenkobuild.xenko.com" && port == 1832)
                        //    await serverSocket.StartClient(host, port, false);
                        //else
                            throw new InvalidOperationException("Trying to redirect to a non-whitelisted host/port");
                        //break;
                    }
                    default:
                        throw new InvalidOperationException("This type of URL is not supported");
                }
            }
            catch (Exception e)
            {
                serverSocketCapturedException = ExceptionDispatchInfo.Capture(e);
            }


            if (serverSocketCapturedException != null)
            {
                try
                {
                    // Notify client that there was an error
                    await clientSocket.WriteStream.WriteInt16Async((short)RouterMessage.ClientServerStarted);
                    await clientSocket.WriteStream.WriteInt32Async(1); // error code Failure
                    await clientSocket.WriteStream.WriteStringAsync(serverSocketCapturedException.SourceException.Message);
                    await clientSocket.WriteStream.FlushAsync();
                }
                finally
                {
                    serverSocketCapturedException.Throw();
                }
            }

            try
            {
                // Notify client that we've found a server for it
                await clientSocket.WriteStream.WriteInt16Async((short)RouterMessage.ClientServerStarted);
                await clientSocket.WriteStream.WriteInt32Async(0); // error code OK
                await clientSocket.WriteStream.FlushAsync();

                // Let's forward clientSocketContext and serverSocketContext
                await await Task.WhenAny(
                    ForwardSocket(clientSocket, serverSocket),
                    ForwardSocket(serverSocket, clientSocket));
            }
            catch
            {
                serverSocket.Dispose();
                throw;
            }
        }

        private async Task<SimpleSocket> SpawnServerFromService(string url, bool task)
        {
            // Ideally we would like to reuse Uri (or some other similar code), but it doesn't work without a Host
            var parameterIndex = url.IndexOf('?');
            var urlWithoutParameters = parameterIndex != -1 ? url.Substring(0, parameterIndex) : url;

            string[] urlSegments;
            string urlParameters;
            RouterHelper.ParseUrl(url, out urlSegments, out urlParameters);

            // Find a matching server
            TaskCompletionSource<Service> serviceTcs;

            lock (registeredServices)
            {
                if (!registeredServices.TryGetValue(urlWithoutParameters, out serviceTcs))
                {
                    if (task) throw new Exception("ConnectionRouter task not found, a task won't spawn a new service on demand instead must be started explicitly.");

                    serviceTcs = new TaskCompletionSource<Service>();
                    registeredServices.Add(urlWithoutParameters, serviceTcs);

                    if (urlSegments.Length < 4)
                    {
                        Log.Error($"{RouterMessage.ClientRequestServer} action URL {url} is invalid");
                        throw new InvalidOperationException();
                    }

                    var packageName = urlSegments[1];
                    var packageVersion = urlSegments[2];
                    var process = urlSegments[3];

                    // Find package
                    var package = PackageStore.Instance.FindLocalPackage(packageName, new PackageVersionRange(new PackageVersion(packageVersion)));
                    if (package == null)
                    {
                        Log.Error($"{RouterMessage.ClientRequestServer} action URL [{url}] could not locate NuGet package");
                        throw new InvalidOperationException();
                    }

                    // Locate executable
                    var servicePath = package.GetFiles().FirstOrDefault(x => string.Compare(Path.GetFileName(x.Path), process, true) == 0)?.FullPath;
                    if (servicePath == null || !File.Exists(servicePath))
                    {
                        Log.Error($"{RouterMessage.ClientRequestServer} action URL [{url}] references a process that doesn't seem to exist");
                        throw new InvalidOperationException();
                    }

                    RunServiceProcessAndLog(servicePath);
                }
            }

            var service = await serviceTcs.Task;

            // Generate connection Guid
            var guid = Guid.NewGuid();
            var serverSocketTcs = new TaskCompletionSource<SimpleSocket>();
            lock (pendingServers)
            {
                pendingServers.Add(guid, serverSocketTcs);
            }

            await service.SendLock.WaitAsync();
            try
            {
                // Notify service that we want it to establish back a new connection to us for this client
                await service.Socket.WriteStream.WriteInt16Async((short)RouterMessage.ServiceRequestServer);
                await service.Socket.WriteStream.WriteStringAsync(url);
                await service.Socket.WriteStream.WriteGuidAsync(guid);
                await service.Socket.WriteStream.FlushAsync();
            }
            finally
            {
                service.SendLock.Release();
            }

            // Should answer within 4 sec
            var ct = new CancellationTokenSource(4000);
            ct.Token.Register(() =>
            {
                if (!serverSocketTcs.Task.IsCompleted)
                {
                    serverSocketTcs.TrySetException(new TimeoutException($"{RouterMessage.ServiceRequestServer} action URL [{url}] could not connect back in time"));
                }
            });

            // Wait for such a server to be available
            return await serverSocketTcs.Task;
        }

        private static Process RunServiceProcessAndLog(string servicePath)
        {
            var process = ShellHelper.RunProcess(servicePath, string.Empty);

            // Create log and notify start
            var logModule = string.Format("{0}:{1}", Path.GetFileNameWithoutExtension(servicePath), process.Id);
            var logger = GlobalLogger.GetLogger(logModule);
            logger.Info("Process started");

            process.OutputDataReceived += (_, args) => logger.Info(args.Data);
            process.ErrorDataReceived += (_, args) => logger.Error(args.Data);
            process.Exited += (_, args) => logger.Info("Process exited");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Let's tie lifetime of spawned process to ours
            // TODO: Move that in a better namespace? (currently a shared file)
            new AttachedChildProcessJob(process);

            return process;
        }

        /// <summary>
        /// Handles ServerStarted messages. It happens when service opened a new "server" connection back to us.
        /// </summary>
        /// <param name="clientSocket">The client socket context.</param>
        /// <returns></returns>
        private async Task HandleMessageServerStarted(SimpleSocket clientSocket)
        {
            var guid = await clientSocket.ReadStream.ReadGuidAsync();
            var errorCode = await clientSocket.ReadStream.ReadInt32Async();
            var errorMessage = (errorCode != 0) ? await clientSocket.ReadStream.ReadStringAsync() : null;

            // Notify any waiter that a server with given GUID is available
            TaskCompletionSource<SimpleSocket> serverSocketTCS;
            lock (pendingServers)
            {
                if (!pendingServers.TryGetValue(guid, out serverSocketTCS))
                {
                    Log.Error("Could not find a matching server Guid");
                    clientSocket.Dispose();
                    return;
                }

                pendingServers.Remove(guid);
            }

            if (errorCode != 0)
                serverSocketTCS.TrySetException(new Exception(errorMessage));
            else
                serverSocketTCS.TrySetResult(clientSocket);
        }

        /// <summary>
        /// Handles ServiceProvideServer messages. It allows service to publicize what "server" they can instantiate.
        /// </summary>
        /// <param name="clientSocket">The client socket context.</param>
        /// <param name="task">If it's a task the service will overwrite old instances</param>
        /// <returns></returns>
        private async Task HandleMessageServiceProvideServer(SimpleSocket clientSocket, bool task)
        {
            var url = await clientSocket.ReadStream.ReadStringAsync();

            lock (registeredServices)
            {
                TaskCompletionSource<Service> service;
                if (task)
                {
                    if (registeredServices.TryGetValue(url, out service))
                    {
                        var result = service.Task.Result;
                        result.Socket.Dispose();
                    }

                    service = new TaskCompletionSource<Service>();
                    registeredServices[url] = service;
                }
                else
                {
                    if (!registeredServices.TryGetValue(url, out service))
                    {
                        service = new TaskCompletionSource<Service>();
                        registeredServices.Add(url, service);
                    }
                }

                service.TrySetResult(new Service(clientSocket));
            }

            // TODO: Handle server disconnections
            //clientSocketContext.Disconnected += 
        }

        private async Task ForwardSocket(SimpleSocket source, SimpleSocket target)
        {
            var buffer = new byte[1024];
            while (true)
            {
                var bufferLength = await source.ReadStream.ReadAsync(buffer, 0, buffer.Length);
                if (bufferLength == 0)
                    throw new IOException("Socket closed");
                await target.WriteStream.WriteAsync(buffer, 0, bufferLength);
                await target.WriteStream.FlushAsync();
            }
        }

        private class Service
        {
            public readonly SimpleSocket Socket;
            public readonly SemaphoreSlim SendLock = new SemaphoreSlim(1);

            public Service(SimpleSocket socket)
            {
                Socket = socket;
            }
        }
    }
}
