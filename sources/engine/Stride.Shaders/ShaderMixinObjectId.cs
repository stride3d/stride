// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Storage;
using Stride.Rendering;
using Stride.Rendering.Data;
using Stride.Shaders.Compiler;

namespace Stride.Shaders
{
    /// <summary>
    /// A helper class to compute a unique object id for a <see cref="ShaderMixinSource"/>.
    /// </summary>
    [DataSerializerGlobal(typeof(ParameterKeyHashSerializer), Profile = "Hash")]
    [DataSerializerGlobal(typeof(ParameterCollectionHashSerializer), Profile = "Hash")]
    public class ShaderMixinObjectId
    {
        private static object generatorLock = new object();
        private static ShaderMixinObjectId generator;

        private readonly NativeMemoryStream memStream;
        private readonly HashSerializationWriter writer;
        private ObjectIdBuilder objectIdBuilder;
        private IntPtr buffer;

        private ShaderMixinObjectId()
        {
            objectIdBuilder = new ObjectIdBuilder();
            buffer = Marshal.AllocHGlobal(65536);
            memStream = new NativeMemoryStream(buffer, 65536);
            writer = new HashSerializationWriter(memStream);
            writer.Context.SerializerSelector = new SerializerSelector("Default", "Hash");
        }

        /// <summary>
        /// Computes a hash <see cref="ObjectId"/> for the specified mixin.
        /// </summary>
        /// <param name="mixin">The mixin.</param>
        /// <param name="mixinParameters">The mixin parameters.</param>
        /// <returns>EffectObjectIds.</returns>
        public static ObjectId Compute(ShaderMixinSource mixin, EffectCompilerParameters effectCompilerParameters)
        {
            lock (generatorLock)
            {
                if (generator == null)
                {
                    generator = new ShaderMixinObjectId();
                }
                return generator.ComputeInternal(mixin, effectCompilerParameters);
            }
        }

        /// <summary>
        /// Computes a hash <see cref="ObjectId"/> for the specified effect and compiler parameters.
        /// </summary>
        /// <param name="effectName">Name of the effect.</param>
        /// <param name="compilerParameters">The compiler parameters.</param>
        /// <returns>
        /// EffectObjectIds.
        /// </returns>
        public static ObjectId Compute(string effectName, CompilerParameters compilerParameters)
        {
            lock (generatorLock)
            {
                if (generator == null)
                {
                    generator = new ShaderMixinObjectId();
                }
                return generator.ComputeInternal(effectName, compilerParameters);
            }
        }

        private unsafe ObjectId ComputeInternal(ShaderMixinSource mixin, EffectCompilerParameters effectCompilerParameters)
        {
            // Write to memory stream
            memStream.Position = 0;
            writer.Write(EffectBytecode.MagicHeader); // Write the effect bytecode magic header
            writer.Write(mixin);

            writer.Write(effectCompilerParameters.Platform);
            writer.Write(effectCompilerParameters.Profile);
            writer.Write(effectCompilerParameters.Debug);
            writer.Write(effectCompilerParameters.OptimizationLevel);

            // Compute hash
            objectIdBuilder.Reset();
            objectIdBuilder.Write((byte*)buffer, (int)memStream.Position);

            return objectIdBuilder.ComputeHash();
        }

        private unsafe ObjectId ComputeInternal(string effectName, CompilerParameters compilerParameters)
        {
            // Write to memory stream
            memStream.Position = 0;
            writer.Write(EffectBytecode.MagicHeader); // Write the effect bytecode magic header
            writer.Write(effectName);

            writer.Write(compilerParameters.EffectParameters.Platform);
            writer.Write(compilerParameters.EffectParameters.Profile);
            writer.Write(compilerParameters.EffectParameters.Debug);
            writer.Write(compilerParameters.EffectParameters.OptimizationLevel);

            writer.Write(compilerParameters);

            // Compute hash
            objectIdBuilder.Reset();
            objectIdBuilder.Write((byte*)buffer, (int)memStream.Position);

            return objectIdBuilder.ComputeHash();
        }
    }
}
