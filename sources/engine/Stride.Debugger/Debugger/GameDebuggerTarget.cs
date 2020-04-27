// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
using Stride.Core.Diagnostics;
using Stride.Core.MicroThreading;
using Stride.Core.Reflection;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.Debugger.Target
{
    public class GameDebuggerTarget : IGameDebuggerTarget
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("GameDebuggerSession");

        /// <summary>
        /// The assembly container, to load assembly without locking main files.
        /// </summary>
        // For now, it uses default one, but later we should probably have one per game debugger session
        private readonly AssemblyContainer assemblyContainer = AssemblyContainer.Default;

        private string projectName = String.Empty;
        private readonly Dictionary<DebugAssembly, Assembly> loadedAssemblies = new Dictionary<DebugAssembly, Assembly>();
        private int currentDebugAssemblyIndex;
        private Game game;

        private readonly ManualResetEvent gameFinished = new ManualResetEvent(true);
        private IGameDebuggerHost host;

        /// <summary>
        /// Flag if exit was requested.
        /// </summary>
        /// <remarks>Field is volatile to avoid compiler optimization that would prevent MainLoop from exiting.</remarks>
        private volatile bool requestedExit;

        public GameDebuggerTarget()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // Make sure this assembly is registered (it contains custom Yaml serializers such as CloneReferenceSerializer)
            // Note: this assembly should not be registered when run by Game Studio
            AssemblyRegistry.Register(typeof(Program).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            lock (loadedAssemblies)
            {
                return loadedAssemblies.Values.FirstOrDefault(x => x.FullName == args.Name);
            }
        }

        /// <inheritdoc/>
        public void Exit()
        {
            requestedExit = true;
        }

        /// <inheritdoc/>
        public DebugAssembly AssemblyLoad(string assemblyPath)
        {
            try
            {
                var assembly = assemblyContainer.LoadAssemblyFromPath(assemblyPath);
                if (assembly == null)
                {
                    Log.Error($"Unexpected error while loading assembly reference [{assemblyPath}] in project [{projectName}]");
                    return DebugAssembly.Empty;
                }

                return CreateDebugAssembly(assembly);
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error while loading assembly reference [{assemblyPath}] in project [{projectName}]", ex);
                return DebugAssembly.Empty;
            }
        }

        /// <inheritdoc/>
        public DebugAssembly AssemblyLoadRaw(byte[] peData, byte[] pdbData)
        {
            try
            {
                lock (loadedAssemblies)
                {
                    var assembly = Assembly.Load(peData, pdbData);
                    return CreateDebugAssembly(assembly);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error while loading assembly reference in project [{projectName}]", ex);
                return DebugAssembly.Empty;
            }
        }

        /// <inheritdoc/>
        public bool AssemblyUpdate(List<DebugAssembly> assembliesToUnregister, List<DebugAssembly> assembliesToRegister)
        {
            Log.Info("Reloading assemblies and updating scripts");

            // Unload and load assemblies in assemblyContainer, serialization, etc...
            lock (loadedAssemblies)
            {
                if (game != null)
                {
                    lock (game.TickLock)
                    {
                        LiveAssemblyReloader.Reload(game, assemblyContainer,
                            assembliesToUnregister.Select(x => loadedAssemblies[x]).ToList(),
                            assembliesToRegister.Select(x => loadedAssemblies[x]).ToList());
                    }
                }
                else
                {
                    LiveAssemblyReloader.Reload(game, assemblyContainer,
                        assembliesToUnregister.Select(x => loadedAssemblies[x]).ToList(),
                        assembliesToRegister.Select(x => loadedAssemblies[x]).ToList());
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public List<string> GameEnumerateTypeNames()
        {
            lock (loadedAssemblies)
            {
                return GameEnumerateTypesHelper().Select(x => x.FullName).ToList();
            }
        }

        /// <inheritdoc/>
        public void GameLaunch(string gameTypeName)
        {
            try
            {
                Log.Info($"Running game with type {gameTypeName}");

                Type gameType;
                lock (loadedAssemblies)
                {
                    gameType = GameEnumerateTypesHelper().FirstOrDefault(x => x.FullName == gameTypeName);
                }

                if (gameType == null)
                    throw new InvalidOperationException($"Could not find type [{gameTypeName}] in project [{projectName}]");

                game = (Game)Activator.CreateInstance(gameType);

                // TODO: Bind database
                Task.Run(() =>
                {
                    gameFinished.Reset();
                    try
                    {
                        using (game)
                        {
                            // Allow scripts to crash, we will still restart them
                            game.Script.Scheduler.PropagateExceptions = false;
                            game.Run();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("Exception while running game", e);
                    }

                    host.OnGameExited();

                    // Notify we are done
                    gameFinished.Set();
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Game [{gameTypeName}] from project [{projectName}] failed to run", ex);
            }
        }

        /// <inheritdoc/>
        public void GameStop()
        {
            if (game == null)
                return;

            game.Exit();

            // Wait for game to actually exit?
            gameFinished.WaitOne();

            game = null;
        }

        private IEnumerable<Type> GameEnumerateTypesHelper()
        {
            // We enumerate custom games, and then typeof(Game) as fallback
            return loadedAssemblies.SelectMany(assembly => assembly.Value.GetTypes().Where(x => typeof(Game).IsAssignableFrom(x)))
                .Concat(Enumerable.Repeat(typeof(Game), 1));
        }

        private DebugAssembly CreateDebugAssembly(Assembly assembly)
        {
            var debugAssembly = new DebugAssembly(++currentDebugAssemblyIndex);
            loadedAssemblies.Add(debugAssembly, assembly);
            return debugAssembly;
        }

        public void MainLoop(IGameDebuggerHost gameDebuggerHost)
        {
            host = gameDebuggerHost;
            string callbackChannelEndpoint = "Stride/Debugger/GameDebuggerTarget/CallbackChannel";
            using (var callbackHost = new NpHost(callbackChannelEndpoint, null, null))
            {
                callbackHost.AddService<IGameDebuggerTarget>(this);
                host.RegisterTarget(callbackChannelEndpoint);

                Log.MessageLogged += Log_MessageLogged;

                // Log suppressed exceptions in scripts
                ScriptSystem.Log.MessageLogged += Log_MessageLogged;
                Scheduler.Log.MessageLogged += Log_MessageLogged;

                Log.Info("Starting debugging session");

                while (!requestedExit)
                {
                    Thread.Sleep(10);
                }
            }
        }

        void Log_MessageLogged(object sender, MessageLoggedEventArgs e)
        {
            var message = e.Message;

            var serializableMessage = message as SerializableLogMessage;
            if (serializableMessage == null)
            {
                var logMessage = message as LogMessage;
                if (logMessage != null)
                {
                    serializableMessage = new SerializableLogMessage(logMessage);
                }
            }

            if (serializableMessage == null)
            {
                throw new InvalidOperationException(@"Unable to process the given log message.");
            }

            host.OnLogMessage(serializableMessage);
        }
    }
}
