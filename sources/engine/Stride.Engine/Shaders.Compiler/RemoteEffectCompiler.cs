// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// Compiles effect remotely on the developer host PC.
    /// </summary>
    internal class RemoteEffectCompiler : EffectCompilerBase
    {
        private readonly DatabaseFileProvider database;
        private readonly RemoteEffectCompilerClient remoteEffectCompilerClient;

        /// <inheritdoc/>
        public override IVirtualFileProvider FileProvider { get; set; }

        public RemoteEffectCompiler(IVirtualFileProvider fileProvider, DatabaseFileProvider database, RemoteEffectCompilerClient remoteEffectCompilerClient)
        {
            FileProvider = fileProvider;
            this.database = database;
            this.remoteEffectCompilerClient = remoteEffectCompilerClient;
        }

        protected override void Destroy()
        {
            remoteEffectCompilerClient.Dispose();

            base.Destroy();
        }

        /// <inheritdoc/>
        public override ObjectId GetShaderSourceHash(string type)
        {
            var url = GetStoragePathFromShaderType(type);
            database.ContentIndexMap.TryGetValue(url, out var shaderSourceId);
            return shaderSourceId;
        }

        /// <inheritdoc/>
        public override TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters = null)
        {
            return CompileAsync(mixinTree, effectParameters);
        }

        private async Task<EffectBytecodeCompilerResult> CompileAsync(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters)
        {
            return await remoteEffectCompilerClient.Compile(mixinTree, effectParameters);
        }
    }
}
