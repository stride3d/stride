// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Serialization;
using Xenko.Engine.Network;
using Xenko.Shaders.Compiler.Internals;

namespace Xenko.Shaders.Compiler
{
    /// <summary>
    /// Used internally by <see cref="RemoteEffectCompiler"/> to compile shaders remotely,
    /// and <see cref="Rendering.EffectSystem.CreateEffectCompiler"/> to record effect requested.
    /// </summary>
    internal class RemoteEffectCompilerClient : IDisposable
    {
        private readonly object lockObject = new object();
        private readonly string packageName;
        private Task<SocketMessageLayer> socketMessageLayerTask;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public RemoteEffectCompilerClient(string packageName)
        {
            this.packageName = packageName;
        }

        public void Dispose()
        {
            // Notify cancellation
            cancellationTokenSource.Cancel();
            if (socketMessageLayerTask != null && socketMessageLayerTask.Status == TaskStatus.RanToCompletion)
            {
                socketMessageLayerTask.Result.Context.Dispose();
                socketMessageLayerTask = null;
            }
        }

        public void NotifyEffectUsed(EffectCompileRequest effectCompileRequest, CompilerResults result)
        {
            if (result.HasErrors)
                return;

            Task.Run(async () =>
            {
                // Silently fails if connection already failed previously
                var socketMessageLayerTask = GetOrCreateConnection(cancellationTokenSource.Token);
                if (socketMessageLayerTask.IsFaulted)
                    return;

                var bytecode = await result.Bytecode.AwaitResult();
                if (bytecode.CompilationLog.HasErrors)
                    return;

                // Ignore everything that has been compiled by the startup cache
                if (bytecode.LoadSource == EffectBytecodeCacheLoadSource.StartupCache)
                    return;

                // Send any effect request remotely (should fail if not connected)
                var socketMessageLayer = await socketMessageLayerTask;

                var memoryStream = new MemoryStream();
                var binaryWriter = new BinarySerializationWriter(memoryStream);
                binaryWriter.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
                binaryWriter.SerializeExtended(effectCompileRequest, ArchiveMode.Serialize, null);

                await socketMessageLayer.Send(new RemoteEffectCompilerEffectRequested { Request = memoryStream.ToArray() });
            });
        }

        public async Task<SocketMessageLayer> Connect(string packageName, CancellationToken cancellationToken)
        {
            var url = $"/service/Xenko.EffectCompilerServer/{XenkoVersion.NuGetVersion}/Xenko.EffectCompilerServer.exe";
            if (packageName != null)
                url += string.Format("?packagename={0}", packageName);

            var socketContext = await RouterClient.RequestServer(url, cancellationToken);

            var socketMessageLayer = new SocketMessageLayer(socketContext, false);

            // Register network VFS
            NetworkVirtualFileProvider.RegisterServer(socketMessageLayer);

            Task.Run(() => socketMessageLayer.MessageLoop());

            return socketMessageLayer;
        }

        public async Task<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters)
        {
            // Make sure we are connected
            // TODO: Handle reconnections, etc...
            var socketMessageLayer = await GetOrCreateConnection(cancellationTokenSource.Token);

            var shaderCompilerAnswer = (RemoteEffectCompilerEffectAnswer)await socketMessageLayer.SendReceiveAsync(new RemoteEffectCompilerEffectRequest
            {
                MixinTree = mixinTree,
                EffectParameters = effectParameters,
            });

            var result = new EffectBytecodeCompilerResult(shaderCompilerAnswer.EffectBytecode, EffectBytecodeCacheLoadSource.JustCompiled);

            foreach (var message in shaderCompilerAnswer.LogMessages)
                result.CompilationLog.Messages.Add(message);

            result.CompilationLog.HasErrors = shaderCompilerAnswer.LogHasErrors;

            return result;
        }

        private async Task<SocketMessageLayer> GetOrCreateConnection(CancellationToken cancellationToken)
        {
            // Lazily connect
            lock (lockObject)
            {
                if (socketMessageLayerTask == null)
                    socketMessageLayerTask = Task.Run(() => Connect(packageName, cancellationToken));
            }

            return await socketMessageLayerTask;
        }
    }
}
