// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP
using System.Collections.Generic;

using Xenko.Shaders;

namespace Xenko.Graphics.ShaderCompiler.Direct3D
{
    public class ShaderCompiler : IShaderCompilerOld
    {
        /// <inheritdoc/>
        public CompilationResult Compile(string shaderSource, string entryPoint, string profile, string sourceFileName = "unknown")
        {
            SharpDX.Configuration.ThrowOnShaderCompileError = false;

            // Compile
            var compilationResult = SharpDX.D3DCompiler.ShaderBytecode.Compile(shaderSource, entryPoint, profile,
                                                                               SharpDX.D3DCompiler.ShaderFlags.OptimizationLevel0 | SharpDX.D3DCompiler.ShaderFlags.Debug,
                                                                               SharpDX.D3DCompiler.EffectFlags.None, null, null, sourceFileName);

            var bytecode = compilationResult.Bytecode;

            var result = new CompilationResult(bytecode == null ? null : new ShaderBytecode(bytecode.Data), compilationResult.HasErrors, compilationResult.Message);

            return result;
        }

        /// <inheritdoc/>
        public ShaderBytecode StripReflection(ShaderBytecode shaderBytecode)
        {
            var bytecode = new SharpDX.D3DCompiler.ShaderBytecode(shaderBytecode).Strip(SharpDX.D3DCompiler.StripFlags.CompilerStripReflectionData);
            return new ShaderBytecode(bytecode);
        }

        /// <inheritdoc/>
        public ShaderReflectionOld GetReflection(ShaderBytecode shaderBytecode)
        {
            var shaderReflection = new SharpDX.D3DCompiler.ShaderReflection(shaderBytecode);
            var shaderReflectionDesc = shaderReflection.Description;

            // Extract reflection data
            var shaderReflectionCopy = new ShaderReflectionOld();
            shaderReflectionCopy.BoundResources = new List<InputBindingDescription>();
            shaderReflectionCopy.ConstantBuffers = new List<ShaderReflectionConstantBuffer>();

            // BoundResources
            for (int i = 0; i < shaderReflectionDesc.BoundResources; ++i)
            {
                var boundResourceDesc = shaderReflection.GetResourceBindingDescription(i);
                shaderReflectionCopy.BoundResources.Add(new InputBindingDescription
                {
                    BindPoint = boundResourceDesc.BindPoint,
                    BindCount = boundResourceDesc.BindCount,
                    Name = boundResourceDesc.Name,
                    Type = (ShaderInputType)boundResourceDesc.Type,
                });
            }

            // ConstantBuffers
            for (int i = 0; i < shaderReflectionDesc.ConstantBuffers; ++i)
            {
                var constantBuffer = shaderReflection.GetConstantBuffer(i);
                var constantBufferDesc = constantBuffer.Description;
                var constantBufferCopy = new ShaderReflectionConstantBuffer
                {
                    Name = constantBufferDesc.Name,
                    Size = constantBufferDesc.Size,
                    Variables = new List<ShaderReflectionVariable>(),
                };

                switch (constantBufferDesc.Type)
                {
                    case SharpDX.D3DCompiler.ConstantBufferType.ConstantBuffer: constantBufferCopy.Type = ConstantBufferType.ConstantBuffer; break;
                    case SharpDX.D3DCompiler.ConstantBufferType.TextureBuffer: constantBufferCopy.Type = ConstantBufferType.TextureBuffer; break;
                    default: constantBufferCopy.Type = ConstantBufferType.Unknown; break;
                }

                // ConstantBuffers variables
                for (int j = 0; j < constantBufferDesc.VariableCount; ++j)
                {
                    var variable = constantBuffer.GetVariable(j);
                    var variableType = variable.GetVariableType().Description;
                    var variableDesc = variable.Description;

                    var variableCopy = new ShaderReflectionVariable
                    {
                        Name = variableDesc.Name,
                        Size = variableDesc.Size,
                        StartOffset = variableDesc.StartOffset,
                        Type = new ShaderReflectionType { Offset = variable.GetVariableType().Description.Offset },
                    };

                    constantBufferCopy.Variables.Add(variableCopy);
                }

                shaderReflectionCopy.ConstantBuffers.Add(constantBufferCopy);
            }

            return shaderReflectionCopy;
        }
    }
}
#endif
