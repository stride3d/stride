// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Shaders.Compiler
{
    public class NullEffectCompiler : EffectCompilerBase
    {
        private readonly DatabaseFileProvider database;

        public NullEffectCompiler(IVirtualFileProvider fileProvider, DatabaseFileProvider database)
        {
            FileProvider = fileProvider;
            this.database = database;
        }

        public override ObjectId GetShaderSourceHash(string type)
        {
            var url = GetStoragePathFromShaderType(type);
            var shaderSourceId = ObjectId.Empty;
            database?.ContentIndexMap.TryGetValue(url, out shaderSourceId);
            return shaderSourceId;
        }

        public override IVirtualFileProvider FileProvider { get; set; }

        public override TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters = null)
        {
            throw new NotSupportedException("Shader Compilation is not allowed at run time on this platform.");
        }
    }
}
