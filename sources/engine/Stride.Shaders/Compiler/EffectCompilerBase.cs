// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// Base class for implementations of <see cref="IEffectCompiler"/>, providing some helper functions.
    /// </summary>
    public abstract class EffectCompilerBase : DisposeBase, IEffectCompiler
    {
        protected EffectCompilerBase()
        {
        }

        /// <summary>
        /// Gets or sets the database file provider, to use for loading effects and shader sources.
        /// </summary>
        /// <value>
        /// The database file provider.
        /// </value>
        public abstract IVirtualFileProvider FileProvider { get; set; }

        public abstract ObjectId GetShaderSourceHash(string type);

        /// <summary>
        /// Remove cached files for modified shaders
        /// </summary>
        /// <param name="modifiedShaders"></param>
        public virtual void ResetCache(HashSet<string> modifiedShaders)
        {
        }

        public CompilerResults Compile(ShaderSource shaderSource, CompilerParameters compilerParameters)
        {
            ShaderMixinSource mixinToCompile;
            var shaderMixinGeneratorSource = shaderSource as ShaderMixinGeneratorSource;

            if (shaderMixinGeneratorSource != null)
            {
                mixinToCompile = ShaderMixinManager.Generate(shaderMixinGeneratorSource.Name, compilerParameters);
            }
            else
            {
                mixinToCompile = shaderSource as ShaderMixinSource;
                var shaderClassSource = shaderSource as ShaderClassCode;

                if (shaderClassSource != null)
                {
                    mixinToCompile = new ShaderMixinSource { Name = shaderClassSource.ClassName };
                    mixinToCompile.Mixins.Add(shaderClassSource);
                }

                if (mixinToCompile == null)
                {
                    throw new ArgumentException("Unsupported ShaderSource type [{0}]. Supporting only ShaderMixinSource/sdfx, ShaderClassSource", "shaderSource");
                }
                if (string.IsNullOrEmpty(mixinToCompile.Name))
                {
                    throw new ArgumentException("ShaderMixinSource must have a name", "shaderSource");
                }
            }

            // Compile the whole mixin tree
            var compilerResults = new CompilerResults { Module = string.Format("EffectCompile [{0}]", mixinToCompile.Name) };
            var bytecode = Compile(mixinToCompile, compilerParameters.EffectParameters, compilerParameters);

            // Since bytecode.Result is a struct, we check if any of its member has been set to know if it's valid
            if (bytecode.Result.CompilationLog != null || bytecode.Task != null)
            {
                if (bytecode.Result.CompilationLog != null)
                {
                    bytecode.Result.CompilationLog.CopyTo(compilerResults);
                }
                compilerResults.Bytecode = bytecode;
                compilerResults.SourceParameters = new CompilerParameters(compilerParameters);
            }
            return compilerResults;
        }

        /// <summary>
        /// Compiles the ShaderMixinSource into a platform bytecode.
        /// </summary>
        /// <param name="mixinTree">The mixin tree.</param>
        /// <param name="effectParameters"></param>
        /// <param name="compilerParameters"></param>
        /// <returns>The platform-dependent bytecode.</returns>
        public abstract TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters);

        public static readonly string DefaultSourceShaderFolder = "shaders";

        public static string GetStoragePathFromShaderType(string type)
        {
            if (type == null) throw new ArgumentNullException("type");
            // TODO: harcoded values, bad bad bad
            return DefaultSourceShaderFolder + "/" + type + ".sdsl";
        }
    }
}
