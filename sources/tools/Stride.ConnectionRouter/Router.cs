// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Engine.Network;

namespace Stride.ConnectionRouter
{
    public class Router
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("Router");

        /// <summary>
        /// How long a process the router started keeps running after its last connection closes, so that a game started again soon reuses it.
        /// </summary>
        private static readonly TimeSpan IdleStopDelay = TimeSpan.FromSeconds(10);

        // The current service of each URL. A service removed from there (a task replaced by a newer one, a service that disconnected) keeps serving its open connections.
        private Dictionary<string, ServiceEntry> registeredServices = new Dictionary<string, ServiceEntry>();
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
            ServiceEntry serviceEntry = null;
            ExceptionDispatchInfo serverSocketCapturedException = null;

            try
            {
                // For now, we handle only "service" URL
                switch (urlSegments[0])
                {
                    case "service":
                    {
                        // From the URL, start service (if not started yet) and ask it to provide a server
                        (serverSocket, serviceEntry) = await SpawnServerFromService(url, false);
                        break;
                    }
                    case "task":
                    {
                        // From the URL, start service (if not started yet) and ask it to provide a server
                        (serverSocket, serviceEntry) = await SpawnServerFromService(url, true);
                        break;
                    }
                    case "redirect":
                    {
                        // Redirect to a IP/port
                        serverSocket = new SimpleSocket();
                        var host = urlSegments[1];
                        var port = int.Parse(urlSegments[2]);

                        // Note: for security reasons, we currently use a whitelist
                        //if (host == "stridebuild.stride3d.net" && port == 1832)
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
            finally
            {
                ReleaseConnection(serviceEntry);
            }
        }

        private async Task<(SimpleSocket Socket, ServiceEntry Service)> SpawnServerFromService(string url, bool task)
        {
            // Ideally we would like to reuse Uri (or some other similar code), but it doesn't work without a Host
            var parameterIndex = url.IndexOf('?');
            var urlWithoutParameters = parameterIndex != -1 ? url.Substring(0, parameterIndex) : url;

            string[] urlSegments;
            string urlParameters;
            RouterHelper.ParseUrl(url, out urlSegments, out urlParameters);

            // Find a matching server
            ServiceEntry serviceEntry;
            lock (registeredServices)
            {
                if (!registeredServices.TryGetValue(urlWithoutParameters, out serviceEntry))
                {
                    if (task)
                        throw new Exception("ConnectionRouter task not found, a task won't spawn a new service on demand instead must be started explicitly.");

                    string servicePath;
                    try
                    {
                        servicePath = LocateService(url, urlSegments);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.Message);
                        throw;
                    }

                    serviceEntry = new ServiceEntry(urlWithoutParameters);
                    serviceEntry.Process = RunServiceProcessAndLog(servicePath, () => OnServiceProcessExited(serviceEntry));

                    // A process that already exited fails this request, and the next one starts a new process
                    if (!serviceEntry.Retired)
                        registeredServices.Add(urlWithoutParameters, serviceEntry);
                }

                // Count the connection right away, so that the process isn't stopped while it is set up
                serviceEntry.ActiveConnections++;
            }

            try
            {
                var service = await serviceEntry.Registered.Task;

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
                catch
                {
                    // The service is gone: the next request starts a new one
                    lock (pendingServers)
                    {
                        pendingServers.Remove(guid);
                    }
                    lock (registeredServices)
                    {
                        RetireService(serviceEntry);
                    }
                    throw;
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
                return (await serverSocketTcs.Task, serviceEntry);
            }
            catch
            {
                ReleaseConnection(serviceEntry);
                throw;
            }
        }

        /// <summary>
        /// Finds the executable of a service from its URL, in the NuGet package the URL names.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="urlSegments">The segments of the URL: "service", package name, package version and executable name.</param>
        /// <returns>The path of the executable.</returns>
        private static string LocateService(string url, string[] urlSegments)
        {
            if (urlSegments.Length < 4)
                throw new InvalidOperationException($"{RouterMessage.ClientRequestServer} action URL {url} is invalid");

            var packageName = urlSegments[1];
            var packageVersion = urlSegments[2];
            var process = urlSegments[3];

            // Find package
            var package = PackageStore.Instance.FindLocalPackage(packageName, new PackageVersionRange(new PackageVersion(packageVersion)));
            if (package == null)
                throw new InvalidOperationException($"{RouterMessage.ClientRequestServer} action URL [{url}] could not locate NuGet package");

            // Locate executable
            var servicePath = package.GetFiles().FirstOrDefault(x => string.Compare(Path.GetFileName(x.Path), process, true) == 0)?.FullPath;
            if (servicePath == null || !File.Exists(servicePath))
                throw new InvalidOperationException($"{RouterMessage.ClientRequestServer} action URL [{url}] references a process that doesn't seem to exist");

            return servicePath;
        }

        private void OnServiceProcessExited(ServiceEntry serviceEntry)
        {
            lock (registeredServices)
            {
                serviceEntry.Exited = true;
                RetireService(serviceEntry);
            }

            // Requests waiting for that process to register fail now instead of waiting forever
            serviceEntry.Registered.TrySetException(new InvalidOperationException($"Service [{serviceEntry.Url}] exited before registering"));
        }

        /// <summary>
        /// Removes a service from <see cref="registeredServices"/>: the next request for its URL starts a new one.
        /// Its process is stopped once its last connection closes.
        /// </summary>
        /// <remarks>Called with the lock on <see cref="registeredServices"/>.</remarks>
        private void RetireService(ServiceEntry serviceEntry)
        {
            if (registeredServices.TryGetValue(serviceEntry.Url, out var currentEntry) && currentEntry == serviceEntry)
                registeredServices.Remove(serviceEntry.Url);

            serviceEntry.Retired = true;
            if (serviceEntry.ActiveConnections == 0)
                StopService(serviceEntry);
        }

        private void ReleaseConnection(ServiceEntry serviceEntry)
        {
            if (serviceEntry == null)
                return;

            lock (registeredServices)
            {
                if (--serviceEntry.ActiveConnections != 0)
                    return;

                if (serviceEntry.Retired)
                {
                    StopService(serviceEntry);
                }
                else if (serviceEntry.Process != null)
                {
                    // A process the router started lives only a short while after its last connection, so that it doesn't keep the package files locked
                    _ = StopServiceWhenIdle(serviceEntry, ++serviceEntry.IdleVersion);
                }
            }
        }

        private async Task StopServiceWhenIdle(ServiceEntry serviceEntry, int idleVersion)
        {
            await Task.Delay(IdleStopDelay);

            lock (registeredServices)
            {
                // Skip it when a connection came in the meantime, even one already closed again: its own release waits again
                if (serviceEntry.ActiveConnections == 0 && serviceEntry.IdleVersion == idleVersion && !serviceEntry.Retired)
                    RetireService(serviceEntry);
            }
        }

        private static void StopService(ServiceEntry serviceEntry)
        {
            if (serviceEntry.Process == null || serviceEntry.Exited)
                return;

            Log.Info($"Stopping process {serviceEntry.Process.Id} of service [{serviceEntry.Url}]: it has no connection left");
            try
            {
                serviceEntry.Process.Kill();
            }
            catch (Exception e) when (e is InvalidOperationException || e is System.ComponentModel.Win32Exception)
            {
                // Already exited
            }
        }

        private static Process RunServiceProcessAndLog(string servicePath, Action exited)
        {
            var process = ShellHelper.RunProcess(servicePath, string.Empty);

            // Create log and notify start
            var logModule = string.Format("{0}:{1}", Path.GetFileNameWithoutExtension(servicePath), process.Id);
            var logger = GlobalLogger.GetLogger(logModule);
            logger.Info("Process started");

            process.OutputDataReceived += (_, args) => logger.Info(args.Data);
            process.ErrorDataReceived += (_, args) => logger.Error(args.Data);
            process.Exited += (_, args) =>
            {
                logger.Info("Process exited");
                exited();
            };
            process.EnableRaisingEvents = true;
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

            ServiceEntry serviceEntry;
            lock (registeredServices)
            {
                registeredServices.TryGetValue(url, out serviceEntry);
                if (task)
                {
                    // A task replaces the previous one
                    if (serviceEntry != null)
                    {
                        if (serviceEntry.Registered.Task.IsCompletedSuccessfully)
                            serviceEntry.Registered.Task.Result.Socket.Dispose();
                        RetireService(serviceEntry);
                        serviceEntry = null;
                    }
                }
                else if (serviceEntry != null && serviceEntry.Registered.Task.IsCompleted)
                {
                    // Another process already serves that URL: keep it
                    Log.Info($"Service [{url}] is already registered, ignoring a second registration");
                    return;
                }

                // A service the router didn't start, or a task
                if (serviceEntry == null)
                {
                    serviceEntry = new ServiceEntry(url);
                    registeredServices.Add(url, serviceEntry);
                }

                serviceEntry.Registered.TrySetResult(new Service(clientSocket));
            }

            // The service writes nothing more on this socket: a read only ends when the service disconnects
            try
            {
                var buffer = new byte[16];
                while (await clientSocket.ReadStream.ReadAsync(buffer, 0, buffer.Length) > 0)
                {
                }
                clientSocket.Dispose();
            }
            finally
            {
                // The next request starts a new process
                Log.Info($"Service [{url}] disconnected");
                lock (registeredServices)
                {
                    RetireService(serviceEntry);
                }
            }
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

        /// <summary>
        /// A service serving a URL: a process the router started, or a process that registered by itself.
        /// </summary>
        /// <remarks>Its counter and flags change with the lock on <see cref="registeredServices"/>.</remarks>
        private class ServiceEntry
        {
            public readonly string Url;

            /// <summary>
            /// Completes when the service registers its control socket.
            /// </summary>
            public readonly TaskCompletionSource<Service> Registered = new TaskCompletionSource<Service>(TaskCreationOptions.RunContinuationsAsynchronously);

            /// <summary>
            /// The process the router started, or <c>null</c>. It is stopped <see cref="IdleStopDelay"/> after its last connection closes.
            /// </summary>
            public Process Process;

            /// <summary>
            /// Counts the times <see cref="ActiveConnections"/> dropped to zero, so that a wait for an idle stop that started before ignores itself.
            /// </summary>
            public int IdleVersion;

            /// <summary>
            /// The number of client connections forwarded to this service, counting the ones being set up.
            /// </summary>
            public int ActiveConnections;

            /// <summary>
            /// Whether this service no longer gets new connections. Its process is stopped once <see cref="ActiveConnections"/> drops to zero.
            /// </summary>
            public bool Retired;

            public bool Exited;

            public ServiceEntry(string url)
            {
                Url = url;
            }
        }
    }
}
