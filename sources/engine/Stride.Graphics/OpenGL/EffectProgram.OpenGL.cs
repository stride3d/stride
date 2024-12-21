// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_OPENGL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Shaders;

namespace Stride.Graphics
{
    class EffectProgram : GraphicsResourceBase
    {
        internal uint ProgramId;

        const string VertexShaderDepthClamp = @"
out float _edc_z;
void main()
{
    _edc_main();

    // transform z to window coordinates
    _edc_z = gl_Position.z / gl_Position.w;
    _edc_z = (gl_DepthRange.diff * _edc_z + gl_DepthRange.near + gl_DepthRange.far) * 0.5;

    // prevent z-clipping
    gl_Position.z = clamp(_edc_z, 0.0, 1.0);
}
";

        const string FragmentShaderDepthClamp = @"
in float _edc_z;
void main()
{
    gl_FragDepth = clamp(_edc_z, 0.0, 1.0);

    _edc_main();
}
";

        private readonly LoggerResult reflectionResult = new LoggerResult();

        private readonly EffectBytecode effectBytecode;

        public Dictionary<string, int> Attributes { get; } = new Dictionary<string, int>();

#if STRIDE_GRAPHICS_API_OPENGLES
        // Fake cbuffer emulation binding
        internal struct Uniform
        {
            public UniformType Type;
            public int UniformIndex;
            public int ConstantBufferSlot;
            public int Offset;
            public int Count;
            public int CompareSize;
        }

        // Start offsets for cbuffer
        private static readonly int[] EmptyConstantBufferOffsets = { 0 };
        internal int[] ConstantBufferOffsets = EmptyConstantBufferOffsets;
#endif

        internal struct Texture
        {
            public int TextureUnit;

            public Texture(int textureUnit)
            {
                TextureUnit = textureUnit;
            }
        }

        internal EffectReflection Reflection;

        internal List<Texture> Textures = new List<Texture>();

        private readonly bool emulateDepthClamp;

        internal EffectProgram(GraphicsDevice device, EffectBytecode bytecode, bool emulateDepthClamp) : base(device)
        {
            effectBytecode = bytecode;
            this.emulateDepthClamp = emulateDepthClamp;

            // TODO OPENGL currently we modify the reflection info; need to find a better way to deal with that
            Reflection = effectBytecode.Reflection;
            CreateShaders();
        }

        protected internal override void OnDestroyed()
        {
            using (GraphicsDevice.UseOpenGLCreationContext())
            {
                GL.DeleteProgram(ProgramId);
            }

            ProgramId = 0;

            base.OnDestroyed();
        }

        private void CreateShaders()
        {
            using (GraphicsDevice.UseOpenGLCreationContext())
            {
                ProgramId = GL.CreateProgram();

                // Attach shaders
                foreach (var shader in effectBytecode.Stages)
                {
                    ShaderType shaderStage;
                    switch (shader.Stage)
                    {
                        case ShaderStage.Vertex:
                            shaderStage = ShaderType.VertexShader;
                            break;
                        case ShaderStage.Pixel:
                            shaderStage = ShaderType.FragmentShader;
                            break;
                        default:
                            throw new Exception("Unsupported shader stage");
                    }

                    var shaderSource = shader.GetDataAsString();

                    //edit the source a little to emulateDepthClamp
                    if (emulateDepthClamp)
                    {
                        var mainPattern = new Regex(@"void\s+main\s*\(\)");
                        if (shaderStage == ShaderType.VertexShader)
                        {
                            //bypass our regular main
                            shaderSource = mainPattern.Replace(shaderSource, @"void _edc_main()");
                            shaderSource += VertexShaderDepthClamp;
                        }
                        else if (shaderStage == ShaderType.FragmentShader)
                        {
                            //bypass our regular main
                            shaderSource = mainPattern.Replace(shaderSource, @"void _edc_main()");
                            shaderSource += FragmentShaderDepthClamp;
                        }
                    }

                    // On OpenGL ES 3.1 and before, texture buffers are likely not supported so we have a fallback using textures
                    shaderSource = shaderSource.Replace("#define texelFetchBufferPlaceholder",
                        GraphicsDevice.HasTextureBuffers
                            ? "#define texelFetchBuffer(sampler, P) texelFetch(sampler, P)"
                            : ("#define samplerBuffer sampler2D\n"
                            + "#define isamplerBuffer isampler2D\n"
                            + "#define usamplerBuffer usampler2D\n"
                            + "#define texelFetchBuffer(sampler, P) texelFetch(sampler, ivec2((P) & 0xFFF, (P) >> 12), 0)\n"));

                    var shaderId = GL.CreateShader(shaderStage);
                    GL.ShaderSource(shaderId, shaderSource);
                    GL.CompileShader(shaderId);

                    int compileStatus;
                    GL.GetShader(shaderId, ShaderParameterName.CompileStatus, out compileStatus);
                    if (compileStatus != 1)
                    {
                        var glErrorMessage = GL.GetShaderInfoLog(shaderId);
                        throw new InvalidOperationException("Error while compiling GLSL shader. [{0}]".ToFormat(glErrorMessage));
                    }

                    GL.AttachShader(ProgramId, shaderId);
                }

#if !STRIDE_GRAPHICS_API_OPENGLES
                // Mark program as retrievable (necessary for later GL.GetProgramBinary).
                GL.ProgramParameter(ProgramId, ProgramParameterPName.ProgramBinaryRetrievableHint, 1);
#endif

                // Link OpenGL program
                GL.LinkProgram(ProgramId);

                // Check link results
                int linkStatus;
                GL.GetProgram(ProgramId, ProgramPropertyARB.LinkStatus, out linkStatus);
                if (linkStatus != 1)
                {
                    var infoLog = GL.GetProgramInfoLog(ProgramId);
                    throw new InvalidOperationException("Error while linking GLSL shaders.\n" + infoLog);
                }

                if (Attributes.Count == 0) // the shader wasn't analyzed yet // TODO Is it possible?
                {
                    // Build attributes list for shader signature
                    int activeAttribCount;
                    GL.GetProgram(ProgramId, ProgramPropertyARB.ActiveAttributes, out activeAttribCount);

                    for (uint activeAttribIndex = 0; activeAttribIndex < activeAttribCount; ++activeAttribIndex)
                    {
                        var attribName = GL.GetActiveAttrib(ProgramId, activeAttribIndex, out var size, out var type);
                        var attribIndex = GL.GetAttribLocation(ProgramId, attribName);
                        Attributes.Add(attribName, attribIndex);
                    }
                }

                CreateReflection(Reflection, effectBytecode.Stages[0].Stage); // need to regenerate the Uniforms on OpenGL ES
            }

            // output the gathered errors
            foreach (var message in reflectionResult.Messages)
                Console.WriteLine(message);
            if (reflectionResult.HasErrors)
                throw new Exception(reflectionResult.Messages.Select(x=>x.ToString()).Aggregate((x,y)=>x+"\n"+y));
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();
            CreateShaders();
            return true;
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            using (GraphicsDevice.UseOpenGLCreationContext())
            {
                GL.DeleteProgram(ProgramId);
            }

            ProgramId = 0;

            base.Destroy();
        }

        /// <summary>
        /// Create or updates the reflection for this shader
        /// </summary>
        /// <param name="effectReflection">the reflection from the hlsl</param>
        /// <param name="stage">the shader pipeline stage</param>
        private void CreateReflection(EffectReflection effectReflection, ShaderStage stage)
        {
            int currentProgram;
            GL.GetInteger(GetPName.CurrentProgram, out currentProgram);
            GL.UseProgram(ProgramId);

            int uniformBlockCount;
            GL.GetProgram(ProgramId, ProgramPropertyARB.ActiveUniformBlocks, out uniformBlockCount);

            var validConstantBuffers = new bool[effectReflection.ConstantBuffers.Count];
            for (uint uniformBlockIndex = 0; uniformBlockIndex < uniformBlockCount; ++uniformBlockIndex)
            {
                // TODO: get previous name to find te actual constant buffer in the reflexion
                GL.GetActiveUniformBlockName(ProgramId, uniformBlockIndex, 1024, out var constantBufferNameLength, out string constantBufferName);

                var constantBufferDescriptionIndex = effectReflection.ConstantBuffers.FindIndex(x => x.Name == constantBufferName);
                if (constantBufferDescriptionIndex == -1)
                {
                    reflectionResult.Error($"Unable to find the constant buffer description [{constantBufferName}]");
                    return;
                }
                var constantBufferIndex = effectReflection.ResourceBindings.FindIndex(x => x.RawName == constantBufferName);
                if (constantBufferIndex == -1)
                {
                    reflectionResult.Error($"Unable to find the constant buffer [{constantBufferName}]");
                    return;
                }

                var constantBufferDescription = effectReflection.ConstantBuffers[constantBufferDescriptionIndex];
                var constantBuffer = effectReflection.ResourceBindings[constantBufferIndex];

                GL.GetActiveUniformBlock(ProgramId, uniformBlockIndex, UniformBlockPName.UniformBlockDataSize, out constantBufferDescription.Size);
                
                int uniformCount;
                GL.GetActiveUniformBlock(ProgramId, uniformBlockIndex, UniformBlockPName.UniformBlockActiveUniforms, out uniformCount);

                // set the binding
                GL.UniformBlockBinding(ProgramId, uniformBlockIndex, uniformBlockIndex);

                // Read uniforms desc
                var uniformIndices = new uint[uniformCount];
                var uniformOffsets = new int[uniformCount];
                var uniformTypes = new int[uniformCount];
                var uniformNames = new string[uniformCount];
                GL.GetActiveUniformBlock(ProgramId, uniformBlockIndex, UniformBlockPName.UniformBlockActiveUniformIndices, MemoryMarshal.Cast<uint, int>(uniformIndices.AsSpan()));
                GL.GetActiveUniforms(ProgramId, (uint)uniformIndices.Length, uniformIndices, UniformPName.UniformOffset, uniformOffsets);
                GL.GetActiveUniforms(ProgramId, (uint)uniformIndices.Length, uniformIndices, UniformPName.UniformType, uniformTypes);
                
                for (int uniformIndex = 0; uniformIndex < uniformIndices.Length; ++uniformIndex)
                {
                    uniformNames[uniformIndex] = GL.GetActiveUniform(ProgramId, uniformIndices[uniformIndex], out var uniformSize, out var uniformType);
                }

                // Reoder by offset
                var indexMapping = uniformIndices.Select((x, i) => new UniformMergeInfo { Offset = uniformOffsets[i], Type = (UniformType)uniformTypes[i], Name = uniformNames[i], NextOffset = 0 }).OrderBy(x => x.Offset).ToArray();
                indexMapping.Last().NextOffset = constantBufferDescription.Size;

                // Fill next offsets
                for (int i = 1; i < indexMapping.Length; ++i)
                {
                    indexMapping[i - 1].NextOffset = indexMapping[i].Offset;
                }

                // Group arrays/structures into one variable (std140 layout is enough for offset determinism inside arrays/structures)
                indexMapping = indexMapping.GroupBy(x =>
                    {
                        // Use only first part of name (ignore structure/array part)
                        var name = x.Name;
                        if (name.Contains(".")) { name = name.Substring(0, name.IndexOf('.')); }
                        if (name.Contains("[")) { name = name.Substring(0, name.IndexOf('[')); }
                        return name;
                    })
                                           .Select(x =>
                                               {
                                                   var result = x.First();
                                                   result.NextOffset = x.Last().NextOffset;

                                                   // Check weither it's an array or a struct
                                                   int dotIndex = result.Name.IndexOf('.');
                                                   int arrayIndex = result.Name.IndexOf('[');

                                                   if (x.Count() > 1 && arrayIndex == -1 && dotIndex == -1)
                                                       throw new InvalidOperationException();

                                                   // TODO: Type processing

                                                   result.Name = x.Key;
                                                   return result;
                                               }).ToArray();

                foreach (var variableIndexGroup in indexMapping)
                {
                    var variableIndex = -1;
                    for (var tentativeIndex = 0; tentativeIndex < constantBufferDescription.Members.Length; ++tentativeIndex)
                    {
                        if (constantBufferDescription.Members[tentativeIndex].RawName == variableIndexGroup.Name)
                        {
                            variableIndex = tentativeIndex;
                            break;
                        }
                    }

                    if (variableIndex == -1)
                    {
                        reflectionResult.Error($"Unable to find uniform [{variableIndexGroup.Name}] in constant buffer [{constantBufferName}]");
                        continue;
                    }
                    var variable = constantBufferDescription.Members[variableIndex];
                    variable.Type.Type = GetTypeFromActiveUniformType(variableIndexGroup.Type);
                    variable.Offset = variableIndexGroup.Offset;
                    variable.Size = variableIndexGroup.NextOffset - variableIndexGroup.Offset;

                    constantBufferDescription.Members[variableIndex] = variable;
                }

                constantBufferDescription.Type = ConstantBufferType.ConstantBuffer;

                constantBuffer.SlotCount = 1; // constant buffers are not arrays
                constantBuffer.SlotStart = (int)uniformBlockIndex;
                constantBuffer.Stage = stage;

                // store the new values
                validConstantBuffers[constantBufferDescriptionIndex] = true;
                effectReflection.ConstantBuffers[constantBufferDescriptionIndex] = constantBufferDescription;
                effectReflection.ResourceBindings[constantBufferIndex] = constantBuffer;
            }
//#endif

            // Remove unecessary cbuffer and resource bindings

            // Register textures, samplers, etc...
            //TODO: (?) non texture/buffer uniform outside of a block
            {
                // Register "NoSampler", required by HLSL=>GLSL translation to support HLSL such as texture.Load().
                var noSampler = new EffectResourceBindingDescription { KeyInfo = { KeyName = "NoSampler" }, RawName = "NoSampler", Class = EffectParameterClass.Sampler, SlotStart = -1, SlotCount = 1 };
                Reflection.ResourceBindings.Add(noSampler);

                int activeUniformCount;
                GL.GetProgram(ProgramId, ProgramPropertyARB.ActiveUniforms, out activeUniformCount);
#if !STRIDE_GRAPHICS_API_OPENGLES
                var uniformTypes = new int[activeUniformCount];
                var uniformIndices = new uint[activeUniformCount];
                for (uint i = 0; i < uniformIndices.Length; ++i)
                    uniformIndices[i] = i;
                GL.GetActiveUniforms(ProgramId, (uint)activeUniformCount, uniformIndices, UniformPName.UniformType, uniformTypes);
#endif

                int textureUnitCount = 0;

                const int sbCapacity = 128;
                var sb = new StringBuilder(sbCapacity);

                for (uint activeUniformIndex = 0; activeUniformIndex < activeUniformCount; ++activeUniformIndex)
                {
                    var uniformName = GL.GetActiveUniform(ProgramId, activeUniformIndex, out var uniformCount, out var uniformType);

#if STRIDE_GRAPHICS_API_OPENGLES
                    //this is a special OpenglES case , it is declared as built in uniform, and the driver will take care of it, we just need to ignore it here
                    if (uniformName.StartsWith("gl_DepthRange"))
                    {
                        continue;
                    }
#endif

                    switch (uniformType)
                    {
                        case UniformType.Sampler1D:
                        case UniformType.Sampler1DShadow:
                        case UniformType.IntSampler1D:
                        case UniformType.UnsignedIntSampler1D:

                        case UniformType.SamplerBuffer:
                        case UniformType.UnsignedIntSamplerBuffer:
                        case UniformType.IntSamplerBuffer:
                        case UniformType.Sampler2D:
                        case UniformType.Sampler2DShadow:
                        case UniformType.Sampler3D: // TODO: remove Texture3D that is not available in OpenGL ES 2
                        case UniformType.SamplerCube:
                        case UniformType.IntSampler2D:
                        case UniformType.IntSampler3D:
                        case UniformType.IntSamplerCube:
                        case UniformType.UnsignedIntSampler2D:
                        case UniformType.UnsignedIntSampler3D:
                        case UniformType.UnsignedIntSamplerCube:
                            var uniformIndex = GL.GetUniformLocation(ProgramId, uniformName);

                            // Temporary way to scan which texture and sampler created this texture_sampler variable (to fix with new HLSL2GLSL converter)

                            var startIndex = -1;
                            var textureReflectionIndex = -1;
                            var samplerReflectionIndex = -1;
                            do
                            {
                                int middlePart = uniformName.IndexOf('_', startIndex + 1);
                                var textureName = middlePart != -1 ? uniformName.Substring(0, middlePart) : uniformName;
                                var samplerName = middlePart != -1 ? uniformName.Substring(middlePart + 1) : null;

                                textureReflectionIndex =
                                    effectReflection.ResourceBindings.FindIndex(x => x.RawName == textureName);
                                samplerReflectionIndex =
                                    effectReflection.ResourceBindings.FindIndex(x => x.RawName == samplerName);

                                if (textureReflectionIndex != -1 && samplerReflectionIndex != -1)
                                    break;

                                startIndex = middlePart;
                            } while (startIndex != -1);

                            if (startIndex == -1 || textureReflectionIndex == -1 || samplerReflectionIndex == -1)
                            {
                                reflectionResult.Error($"Unable to find sampler and texture corresponding to [{uniformName}]");
                                continue; // Error
                            }

                            var textureReflection = effectReflection.ResourceBindings[textureReflectionIndex];
                            var samplerReflection = effectReflection.ResourceBindings[samplerReflectionIndex];

                            // Contrary to Direct3D, samplers and textures are part of the same object in OpenGL
                            // Since we are exposing the Direct3D representation, a single sampler parameter key can be used for several textures, a single texture can be used with several samplers.
                            // When such a case is detected, we need to duplicate the resource binding.
                            textureReflectionIndex = GetReflexionIndex(textureReflection, textureReflectionIndex, effectReflection.ResourceBindings);
                            samplerReflectionIndex = GetReflexionIndex(samplerReflection, samplerReflectionIndex, effectReflection.ResourceBindings);

                            // Update texture uniform mapping
                            GL.Uniform1(uniformIndex, textureUnitCount);
                            
                            textureReflection.Stage = stage;
                            //textureReflection.Param.RawName = uniformName;
                            textureReflection.Type = GetTypeFromActiveUniformType(uniformType);
                            textureReflection.Class = EffectParameterClass.ShaderResourceView;
                            textureReflection.SlotStart = textureUnitCount;
                            textureReflection.SlotCount = 1; // TODO: texture arrays

                            samplerReflection.Stage = stage;
                            samplerReflection.Class = EffectParameterClass.Sampler;
                            samplerReflection.SlotStart = textureUnitCount;
                            samplerReflection.SlotCount = 1; // TODO: texture arrays

                            effectReflection.ResourceBindings[textureReflectionIndex] = textureReflection;
                            effectReflection.ResourceBindings[samplerReflectionIndex] = samplerReflection;

                            Textures.Add(new Texture(textureUnitCount));
                            
                            textureUnitCount++;
                            break;
                    }
                }

                // Remove any optimized resource binding
                effectReflection.ResourceBindings.RemoveAll(x => x.SlotStart == -1);
                effectReflection.ConstantBuffers = effectReflection.ConstantBuffers.Where((cb, i) => validConstantBuffers[i]).ToList();
            }

            GL.UseProgram((uint)currentProgram);
        }

        /// <summary>
        /// Inserts the data in the list if this is a copy of a previously set one.
        /// </summary>
        /// <param name="data">The  data.</param>
        /// <param name="index">The index in the list.</param>
        /// <param name="bindings">The list of bindings.</param>
        /// <returns>The new index of the data.</returns>
        private static int GetReflexionIndex(EffectResourceBindingDescription data, int index, FastList<EffectResourceBindingDescription> bindings)
        {
            if (data.SlotCount != 0)
            {
                // slot count has been specified, this means that this resource was already configured
                // We have to create a new entry for the data
                var newIndex = bindings.Count;
                bindings.Add(data);
                return newIndex;
            }
            return index;
        }

        private static int GetCountFromActiveUniformType(UniformType type)
        {
            switch (type)
            {
                case UniformType.Int:
                case UniformType.Float:
                case UniformType.Bool:
                    return 1;
                case UniformType.IntVec2:
                case UniformType.UnsignedIntVec2:
                case UniformType.FloatVec2:
                case UniformType.BoolVec2:
                    return 2;
                case UniformType.IntVec3:
                case UniformType.UnsignedIntVec3:
                case UniformType.FloatVec3:
                case UniformType.BoolVec3:
                    return 3;
                case UniformType.IntVec4:
                case UniformType.UnsignedIntVec4:
                case UniformType.FloatVec4:
                case UniformType.BoolVec4:
                case UniformType.FloatMat2:
                    return 4;
                case UniformType.FloatMat2x3:
                case UniformType.FloatMat3x2:
                    return 6;
                case UniformType.FloatMat2x4:
                case UniformType.FloatMat4x2:
                    return 8;
                case UniformType.FloatMat3:
                    return 9;
                case UniformType.FloatMat3x4:
                case UniformType.FloatMat4x3:
                    return 12;
                case UniformType.FloatMat4:
                    return 16;
                
                case UniformType.Sampler2D:
                case UniformType.SamplerCube:
                case UniformType.Sampler3D:
                case UniformType.Sampler2DShadow:
                case UniformType.SamplerCubeShadow:
                case UniformType.IntSampler2D:
                case UniformType.IntSampler3D:
                case UniformType.IntSamplerCube:
                case UniformType.UnsignedIntSampler2D:
                case UniformType.UnsignedIntSampler3D:
                case UniformType.UnsignedIntSamplerCube:
                case UniformType.Sampler2DArray:
                case UniformType.Sampler2DArrayShadow:
                case UniformType.IntSampler2DArray:
                case UniformType.UnsignedIntSampler2DArray:
#if !STRIDE_GRAPHICS_API_OPENGLES
                case UniformType.Sampler1D:
                case UniformType.Sampler1DShadow:
                case UniformType.Sampler2DRect:
                case UniformType.Sampler2DRectShadow:
                case UniformType.IntSampler1D:
                case UniformType.IntSampler2DRect:
                case UniformType.UnsignedIntSampler1D:
                case UniformType.UnsignedIntSampler2DRect:
                case UniformType.Sampler1DArray:
                case UniformType.Sampler1DArrayShadow:
                case UniformType.IntSampler1DArray:
                case UniformType.UnsignedIntSampler1DArray:
#endif
                    return 1;
#if !STRIDE_GRAPHICS_API_OPENGLES
                case UniformType.SamplerBuffer:
                case UniformType.IntSamplerBuffer:
                case UniformType.UnsignedIntSamplerBuffer:
                    return 1;
                case UniformType.Sampler2DMultisample:
                case UniformType.IntSampler2DMultisample:
                case UniformType.UnsignedIntSampler2DMultisample:
                    return 1;
                case UniformType.Sampler2DMultisampleArray:
                case UniformType.IntSampler2DMultisampleArray:
                case UniformType.UnsignedIntSampler2DMultisampleArray:
#endif
                    return 1;
                default:
                    //TODO: log error ?
                    return 0;
            }
        }

        private static EffectParameterClass GetClassFromActiveUniformType(UniformType type)
        {
            switch (type)
            {
                case UniformType.Int:
                case UniformType.Float:
                case UniformType.Bool:
                    return EffectParameterClass.Scalar;
                case UniformType.FloatVec2:
                case UniformType.FloatVec3:
                case UniformType.FloatVec4:
                case UniformType.IntVec2:
                case UniformType.IntVec3:
                case UniformType.IntVec4:
                case UniformType.BoolVec2:
                case UniformType.BoolVec3:
                case UniformType.BoolVec4:
                case UniformType.UnsignedIntVec2:
                case UniformType.UnsignedIntVec3:
                case UniformType.UnsignedIntVec4:
                    return EffectParameterClass.Vector;
                case UniformType.FloatMat2:
                case UniformType.FloatMat3:
                case UniformType.FloatMat4:
                case UniformType.FloatMat2x3:
                case UniformType.FloatMat2x4:
                case UniformType.FloatMat3x2:
                case UniformType.FloatMat3x4:
                case UniformType.FloatMat4x2:
                case UniformType.FloatMat4x3:
                    return EffectParameterClass.MatrixColumns;
                    //return EffectParameterClass.MatrixRows;
                    //return EffectParameterClass.Vector;
                case UniformType.Sampler2D:
                case UniformType.SamplerCube:
                case UniformType.Sampler3D:
                case UniformType.Sampler2DShadow:
                case UniformType.Sampler2DArray:
                case UniformType.Sampler2DArrayShadow:
                case UniformType.SamplerCubeShadow:
                case UniformType.IntSampler2D:
                case UniformType.IntSampler3D:
                case UniformType.IntSamplerCube:
                case UniformType.IntSampler2DArray:
                case UniformType.UnsignedIntSampler2D:
                case UniformType.UnsignedIntSampler3D:
                case UniformType.UnsignedIntSamplerCube:
                case UniformType.UnsignedIntSampler2DArray:
#if !STRIDE_GRAPHICS_API_OPENGLES
                case UniformType.Sampler1D:
                case UniformType.Sampler1DShadow:
                case UniformType.Sampler2DRect:
                case UniformType.Sampler2DRectShadow:
                case UniformType.Sampler1DArray:
                case UniformType.SamplerBuffer:
                case UniformType.Sampler1DArrayShadow:
                case UniformType.IntSampler1D:
                case UniformType.IntSampler2DRect:
                case UniformType.IntSampler1DArray:
                case UniformType.IntSamplerBuffer:
                case UniformType.UnsignedIntSampler1D:
                case UniformType.UnsignedIntSampler2DRect:
                case UniformType.UnsignedIntSampler1DArray:
                case UniformType.UnsignedIntSamplerBuffer:
                case UniformType.Sampler2DMultisample:
                case UniformType.IntSampler2DMultisample:
                case UniformType.UnsignedIntSampler2DMultisample:
                case UniformType.Sampler2DMultisampleArray:
                case UniformType.IntSampler2DMultisampleArray:
                case UniformType.UnsignedIntSampler2DMultisampleArray:
#endif
                    return EffectParameterClass.TextureBuffer;
                default:
                    //TODO: log error ?
                    return EffectParameterClass.Object;
            }
        }

        private static EffectParameterType GetTypeFromActiveUniformType(UniformType type)
        {
            switch (type)
            {
                case UniformType.Int:
                case UniformType.IntVec2:
                case UniformType.IntVec3:
                case UniformType.IntVec4:
                    return EffectParameterType.Int;
                case UniformType.Float:
                case UniformType.FloatVec2:
                case UniformType.FloatVec3:
                case UniformType.FloatVec4:
                case UniformType.FloatMat2:
                case UniformType.FloatMat3:
                case UniformType.FloatMat4:
                case UniformType.FloatMat2x3:
                case UniformType.FloatMat2x4:
                case UniformType.FloatMat3x2:
                case UniformType.FloatMat3x4:
                case UniformType.FloatMat4x2:
                case UniformType.FloatMat4x3:
                    return EffectParameterType.Float;
                case UniformType.Bool:
                case UniformType.BoolVec2:
                case UniformType.BoolVec3:
                case UniformType.BoolVec4:
                    return EffectParameterType.Bool;
                case UniformType.UnsignedIntVec2:
                case UniformType.UnsignedIntVec3:
                case UniformType.UnsignedIntVec4:
                    return EffectParameterType.UInt;
#if !STRIDE_GRAPHICS_API_OPENGLES
                case UniformType.Sampler1D:
                case UniformType.Sampler1DShadow:
                case UniformType.IntSampler1D:
                case UniformType.UnsignedIntSampler1D:
                    return EffectParameterType.Texture1D;
#endif
                case UniformType.Sampler2D:
                case UniformType.Sampler2DShadow:
                case UniformType.IntSampler2D:
                case UniformType.UnsignedIntSampler2D:
#if !STRIDE_GRAPHICS_API_OPENGLES
                case UniformType.Sampler2DRect:
                case UniformType.Sampler2DRectShadow:
                case UniformType.IntSampler2DRect:
                case UniformType.UnsignedIntSampler2DRect:
#endif
                    return EffectParameterType.Texture2D;
                case UniformType.Sampler3D:
                case UniformType.IntSampler3D:
                case UniformType.UnsignedIntSampler3D:
                    return EffectParameterType.Texture3D;
                case UniformType.SamplerCube:
                case UniformType.SamplerCubeShadow:
                case UniformType.IntSamplerCube:
                case UniformType.UnsignedIntSamplerCube:
                    return EffectParameterType.TextureCube;
                case UniformType.Sampler2DArray:
                case UniformType.Sampler2DArrayShadow:
                case UniformType.IntSampler2DArray:
                case UniformType.UnsignedIntSampler2DArray:
                    return EffectParameterType.Texture2DArray;
#if !STRIDE_GRAPHICS_API_OPENGLES
                case UniformType.Sampler1DArray:
                case UniformType.Sampler1DArrayShadow:
                case UniformType.IntSampler1DArray:
                case UniformType.UnsignedIntSampler1DArray:
                    return EffectParameterType.Texture1DArray;
                case UniformType.SamplerBuffer:
                case UniformType.IntSamplerBuffer:
                case UniformType.UnsignedIntSamplerBuffer:
                    return EffectParameterType.TextureBuffer;
                case UniformType.Sampler2DMultisample:
                case UniformType.IntSampler2DMultisample:
                case UniformType.UnsignedIntSampler2DMultisample:
                    return EffectParameterType.Texture2DMultisampled;
                case UniformType.Sampler2DMultisampleArray:
                case UniformType.IntSampler2DMultisampleArray:
                case UniformType.UnsignedIntSampler2DMultisampleArray:
                    return EffectParameterType.Texture2DMultisampledArray;
#endif
                default:
                    //TODO: log error ?
                    return EffectParameterType.Void;
            }
        }

        class UniformMergeInfo
        {
            public UniformType Type;
            public int Offset;
            public int NextOffset;
            public string Name;
        }
    }
}
#endif
