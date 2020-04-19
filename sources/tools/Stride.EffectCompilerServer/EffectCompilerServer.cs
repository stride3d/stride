// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Assets.Effect;
using Stride.ConnectionRouter;
using Stride.Engine.Network;
using Stride.Shaders.Compiler;
using Stride.Shaders.Compiler.Internals;

namespace Stride.EffectCompilerServer
{
    /// <summary>
    /// Shader compiler host (over network)
    /// </summary>
    public class EffectCompilerServer : RouterServiceServer
    {
        private readonly Dictionary<string, SocketMessageLayer> gameStudioPerPackageName = new Dictionary<string, SocketMessageLayer>();

        public EffectCompilerServer() : base($"/service/Stride.EffectCompilerServer/{StrideVersion.NuGetVersion}/Stride.EffectCompilerServer.exe")
        {
            // TODO: Asynchronously initialize Irony grammars to improve first compilation request performance?
        }

        /// <inheritdoc/>
        protected override async void HandleClient(SimpleSocket clientSocket, string url)
        {
            string[] urlSegments;
            string urlParameters;
            RouterHelper.ParseUrl(url, out urlSegments, out urlParameters);
            var parameters = RouterHelper.ParseQueryString(urlParameters);
            var mode = parameters["mode"];

            // We accept everything
            await AcceptConnection(clientSocket);

            var socketMessageLayer = new SocketMessageLayer(clientSocket, true);

            string packageName = parameters["packagename"];

            if (mode == "gamestudio")
            {
                Console.WriteLine(@"GameStudio mode started!");

                if (packageName == null)
                    return;

                lock (gameStudioPerPackageName)
                {
                    gameStudioPerPackageName[packageName] = socketMessageLayer;
                }
            }
            else
            {

                Console.WriteLine(@"Client connected");

                // Make a VFS that will access remotely the DatabaseFileProvider
                // TODO: Is that how we really want to do that in the future?
                var networkVFS = new NetworkVirtualFileProvider(socketMessageLayer, "/asset");
                VirtualFileSystem.RegisterProvider(networkVFS);

                // Create an effect compiler per connection
                var effectCompiler = new EffectCompiler(networkVFS);
                // TODO: This should come from an "init" packet
                effectCompiler.SourceDirectories.Add(EffectCompilerBase.DefaultSourceShaderFolder);
                effectCompiler.FileProvider = networkVFS;

                socketMessageLayer.AddPacketHandler<RemoteEffectCompilerEffectRequest>(packet => ShaderCompilerRequestHandler(socketMessageLayer, effectCompiler, packet));

                socketMessageLayer.AddPacketHandler<RemoteEffectCompilerEffectRequested>(packet =>
                {
                    if (packageName == null)
                        return;

                    SocketMessageLayer gameStudio;
                    lock (gameStudioPerPackageName)
                    {
                        if (!gameStudioPerPackageName.TryGetValue(packageName, out gameStudio))
                            return;
                    }

                    // Forward to game studio
                    gameStudio.Send(packet);
                });
            }

            Task.Run(() => socketMessageLayer.MessageLoop());
        }

        private static async Task ShaderCompilerRequestHandler(SocketMessageLayer socketMessageLayer, EffectCompiler effectCompiler, RemoteEffectCompilerEffectRequest remoteEffectCompilerEffectRequest)
        {
            // Yield so that this socket can continue its message loop to answer to shader file request
            // TODO: maybe not necessary anymore with RouterServiceServer?
            await Task.Yield();

            Console.WriteLine($"Compiling shader: {remoteEffectCompilerEffectRequest.MixinTree.Name}");

            // A shader has been requested, compile it (asynchronously)!
            var precompiledEffectShaderPass = await effectCompiler.Compile(remoteEffectCompilerEffectRequest.MixinTree, remoteEffectCompilerEffectRequest.EffectParameters, null).AwaitResult();

            // Send compiled shader
            await socketMessageLayer.Send(new RemoteEffectCompilerEffectAnswer
            {
                StreamId = remoteEffectCompilerEffectRequest.StreamId,
                LogMessages = precompiledEffectShaderPass.CompilationLog.Messages.Select(x => new SerializableLogMessage((LogMessage)x)).ToList(),
                LogHasErrors = precompiledEffectShaderPass.CompilationLog.HasErrors,
                EffectBytecode = precompiledEffectShaderPass.Bytecode,
            });
        }
    }
}
