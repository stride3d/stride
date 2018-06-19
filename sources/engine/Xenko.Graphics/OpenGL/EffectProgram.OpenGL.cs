// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_OPENGL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenTK.Graphics;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Serialization;
using Xenko.Shaders;
#if XENKO_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif


namespace Xenko.Graphics
{
    class EffectProgram : GraphicsResourceBase
    {
#if XENKO_GRAPHICS_API_OPENGLES
        // The ProgramParameter.ActiveUniformBlocks enum is not defined in OpenTK for OpenGL ES
        private const GetProgramParameterName XkActiveUniformBlocks = (GetProgramParameterName)0x8A36;
        private const ActiveUniformType FloatMat3x2 = (ActiveUniformType)0x8B67;
#else
        private const GetProgramParameterName XkActiveUniformBlocks = GetProgramParameterName.ActiveUniformBlocks;
        private const ActiveUniformType FloatMat3x2 = ActiveUniformType.FloatMat3x2;
#endif

        internal int ProgramId;

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

#if XENKO_GRAPHICS_API_OPENGLES
        // Fake cbuffer emulation binding
        internal struct Uniform
        {
            public ActiveUniformType Type;
            public int UniformIndex;
            public int ConstantBufferSlot;
            public int Offset;
            public int Count;
            public int CompareSize;
        }

        // Start offsets for cbuffer
        private static readonly int[] EmptyConstantBufferOffsets = { 0 };
        internal int[] ConstantBufferOffsets = EmptyConstantBufferOffsets;

        internal byte[] BoundUniforms;
        internal List<Uniform> Uniforms = new List<Uniform>();
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

#if XENKO_GRAPHICS_API_OPENGLES
                    var shaderSources = BinarySerialization.Read<ShaderLevelBytecode>(shader.Data);
                    var shaderSource = GraphicsDevice.IsOpenGLES2 ? shaderSources.DataES2 : shaderSources.DataES3;
#else
                    var shaderSource = shader.GetDataAsString();
#endif
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
                    GL.GetShader(shaderId, ShaderParameter.CompileStatus, out compileStatus);
                    if (compileStatus != 1)
                    {
                        var glErrorMessage = GL.GetShaderInfoLog(shaderId);
                        throw new InvalidOperationException("Error while compiling GLSL shader. [{0}]".ToFormat(glErrorMessage));
                    }

                    GL.AttachShader(ProgramId, shaderId);
                }

#if !XENKO_GRAPHICS_API_OPENGLES
                // Mark program as retrievable (necessary for later GL.GetProgramBinary).
                GL.ProgramParameter(ProgramId, ProgramParameterName.ProgramBinaryRetrievableHint, 1);
#endif

                // Link OpenGL program
                GL.LinkProgram(ProgramId);

                // Check link results
                int linkStatus;
                GL.GetProgram(ProgramId, GetProgramParameterName.LinkStatus, out linkStatus);
                if (linkStatus != 1)
                {
                    var infoLog = GL.GetProgramInfoLog(ProgramId);
                    throw new InvalidOperationException("Error while linking GLSL shaders.\n" + infoLog);
                }

                if (Attributes.Count == 0) // the shader wasn't analyzed yet // TODO Is it possible?
                {
                    // Build attributes list for shader signature
                    int activeAttribCount;
                    GL.GetProgram(ProgramId, GetProgramParameterName.ActiveAttributes, out activeAttribCount);

                    for (int activeAttribIndex = 0; activeAttribIndex < activeAttribCount; ++activeAttribIndex)
                    {
                        int size;
                        ActiveAttribType type;
                        var attribName = GL.GetActiveAttrib(ProgramId, activeAttribIndex, out size, out type);
                        var attribIndex = GL.GetAttribLocation(ProgramId, attribName);
                        Attributes.Add(attribName, attribIndex);
                    }
                }

                CreateReflection(Reflection, effectBytecode.Stages[0].Stage); // need to regenerate the Uniforms on OpenGL ES

#if XENKO_GRAPHICS_API_OPENGLES
                // Allocate a buffer that can cache all the bound parameters
                BoundUniforms = new byte[ConstantBufferOffsets[ConstantBufferOffsets.Length - 1]];
#endif
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
            GL.GetProgram(ProgramId, XkActiveUniformBlocks, out uniformBlockCount);

            var validConstantBuffers = new bool[effectReflection.ConstantBuffers.Count];
            for (int uniformBlockIndex = 0; uniformBlockIndex < uniformBlockCount; ++uniformBlockIndex)
            {
                // TODO: get previous name to find te actual constant buffer in the reflexion
#if XENKO_GRAPHICS_API_OPENGLES
                const int sbCapacity = 128;
                int length;
                var sb = new StringBuilder(sbCapacity);
                GL.GetActiveUniformBlockName(ProgramId, uniformBlockIndex, sbCapacity, out length, sb);
                var constantBufferName = sb.ToString();
#else
                var constantBufferName = GL.GetActiveUniformBlockName(ProgramId, uniformBlockIndex);
#endif

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

                GL.GetActiveUniformBlock(ProgramId, uniformBlockIndex, ActiveUniformBlockParameter.UniformBlockDataSize, out constantBufferDescription.Size);
                
                int uniformCount;
                GL.GetActiveUniformBlock(ProgramId, uniformBlockIndex, ActiveUniformBlockParameter.UniformBlockActiveUniforms, out uniformCount);

                // set the binding
                GL.UniformBlockBinding(ProgramId, uniformBlockIndex, uniformBlockIndex);

                // Read uniforms desc
                var uniformIndices = new int[uniformCount];
                var uniformOffsets = new int[uniformCount];
                var uniformTypes = new int[uniformCount];
                var uniformNames = new string[uniformCount];
                GL.GetActiveUniformBlock(ProgramId, uniformBlockIndex, ActiveUniformBlockParameter.UniformBlockActiveUniformIndices, uniformIndices);
                GL.GetActiveUniforms(ProgramId, uniformIndices.Length, uniformIndices, ActiveUniformParameter.UniformOffset, uniformOffsets);
                GL.GetActiveUniforms(ProgramId, uniformIndices.Length, uniformIndices, ActiveUniformParameter.UniformType, uniformTypes);
                
                for (int uniformIndex = 0; uniformIndex < uniformIndices.Length; ++uniformIndex)
                {
#if XENKO_GRAPHICS_API_OPENGLES
                    int size;
                    ActiveUniformType aut;
                    GL.GetActiveUniform(ProgramId, uniformIndices[uniformIndex], sbCapacity, out length, out size, out aut, sb);
                    uniformNames[uniformIndex] = sb.ToString();
#else
                    uniformNames[uniformIndex] = GL.GetActiveUniformName(ProgramId, uniformIndices[uniformIndex]);
#endif
                }

                // Reoder by offset
                var indexMapping = uniformIndices.Select((x, i) => new UniformMergeInfo { Offset = uniformOffsets[i], Type = (ActiveUniformType)uniformTypes[i], Name = uniformNames[i], NextOffset = 0 }).OrderBy(x => x.Offset).ToArray();
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
                constantBuffer.SlotStart = uniformBlockIndex;
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
                GL.GetProgram(ProgramId, GetProgramParameterName.ActiveUniforms, out activeUniformCount);
#if !XENKO_GRAPHICS_API_OPENGLES
                var uniformTypes = new int[activeUniformCount];
                GL.GetActiveUniforms(ProgramId, activeUniformCount, Enumerable.Range(0, activeUniformCount).ToArray(), ActiveUniformParameter.UniformType, uniformTypes);
#endif

                int textureUnitCount = 0;

                const int sbCapacity = 128;
                var sb = new StringBuilder(sbCapacity);

                for (int activeUniformIndex = 0; activeUniformIndex < activeUniformCount; ++activeUniformIndex)
                {
#if !XENKO_GRAPHICS_API_OPENGLES
                    var uniformType = (ActiveUniformType)uniformTypes[activeUniformIndex];
                    var uniformName = GL.GetActiveUniformName(ProgramId, activeUniformIndex);
#else
                    ActiveUniformType uniformType;
                    int uniformCount;
                    int length;
                    GL.GetActiveUniform(ProgramId, activeUniformIndex, sbCapacity, out length, out uniformCount, out uniformType, sb);
                    var uniformName = sb.ToString();

                    //this is a special OpenglES case , it is declared as built in uniform, and the driver will take care of it, we just need to ignore it here
                    if (uniformName.StartsWith("gl_DepthRange"))
                    {
                        continue;
                    }
#endif

#if XENKO_GRAPHICS_API_OPENGLES
                    // Process uniforms
                    if (GraphicsDevice.IsOpenGLES2)
                    {
                        switch (uniformType)
                        {
                            case ActiveUniformType.Bool:
                            case ActiveUniformType.Int:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(int)*1, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.BoolVec2:
                            case ActiveUniformType.IntVec2:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(int)*2, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.BoolVec3:
                            case ActiveUniformType.IntVec3:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(int)*3, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.BoolVec4:
                            case ActiveUniformType.IntVec4:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(int)*4, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.Float:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(float)*1, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.FloatVec2:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(float)*2, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.FloatVec3:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(float)*3, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.FloatVec4:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(float)*4, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.FloatMat4:
                                AddUniform(effectReflection, validConstantBuffers, sizeof(float)*4*4, uniformCount, uniformName, uniformType);
                                break;
                            case ActiveUniformType.FloatMat2:
                            case ActiveUniformType.FloatMat3:
                                throw new NotImplementedException();
                        }
                    }
#endif

                    switch (uniformType)
                    {
#if !XENKO_GRAPHICS_API_OPENGLES
                        case ActiveUniformType.Sampler1D:
                        case ActiveUniformType.Sampler1DShadow:
                        case ActiveUniformType.IntSampler1D:
                        case ActiveUniformType.UnsignedIntSampler1D:

                        case ActiveUniformType.SamplerBuffer:
                        case ActiveUniformType.UnsignedIntSamplerBuffer:
                        case ActiveUniformType.IntSamplerBuffer:
#endif
                        case ActiveUniformType.Sampler2D:
                        case ActiveUniformType.Sampler2DShadow:
                        case ActiveUniformType.Sampler3D: // TODO: remove Texture3D that is not available in OpenGL ES 2
                        case ActiveUniformType.SamplerCube:
                        case ActiveUniformType.IntSampler2D:
                        case ActiveUniformType.IntSampler3D:
                        case ActiveUniformType.IntSamplerCube:
                        case ActiveUniformType.UnsignedIntSampler2D:
                        case ActiveUniformType.UnsignedIntSampler3D:
                        case ActiveUniformType.UnsignedIntSamplerCube:
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

            GL.UseProgram(currentProgram);
        }

#if XENKO_GRAPHICS_API_OPENGLES
        struct UniformPart
        {
            public int Start;
            public int Count;
            public int Indexer;
        }

        private void AddUniform(EffectReflection effectReflection, bool[] validConstantBuffers, int uniformSize, int uniformCount, string uniformName, ActiveUniformType uniformType)
        {
            // OpenGL ES 2 is adding uniform for each cbuffer member, so we need to remove array and struct indexers so that we can identify it in cbuffer and find offset
            var uniformParts = new List<UniformPart>(8);
            var uniformLastStart = 0;
            for (var index = 0; index <= uniformName.Length; index++)
            {
                char c = index == uniformName.Length ? '.' : uniformName[index]; // Treat string end same as '.'
                if (c == '.' || c == '[')
                {
                    var uniformPart = new UniformPart { Start = uniformLastStart, Count = index - uniformLastStart, Indexer = -1 };

                    // Read array index (if any)
                    if (c == '[')
                    {
                        var indexerStart = ++index;
                        while (uniformName[index] != ']')
                            index++;
                        // TODO: Avoid substring
                        uniformPart.Indexer = int.Parse(uniformName.Substring(indexerStart, index - indexerStart));
                        index++;
                    }

                    uniformParts.Add(uniformPart);
                    uniformLastStart = index + 1;
                }
            }

            var variableName = uniformName.Substring(0, uniformParts[0].Count);

            // check that this uniform is in a constant buffer
            int indexOfConstantBuffer = -1;
            int indexOfMember = -1;
            EffectConstantBufferDescription constantBufferDescription = null;
            for (int cbIndex = 0; cbIndex < effectReflection.ConstantBuffers.Count; cbIndex++)
            {
                var currentConstantBuffer = effectReflection.ConstantBuffers[cbIndex];
                for (int index = 0; index < currentConstantBuffer.Members.Length; index++)
                {
                    var member = currentConstantBuffer.Members[index];
                    if (member.RawName.Equals(variableName))
                    {
                        indexOfConstantBuffer = cbIndex;
                        indexOfMember = index;
                        constantBufferDescription = currentConstantBuffer;
                        break;
                    }
                }
                if (constantBufferDescription != null)
                    break;
            }

            if (constantBufferDescription == null)
            {
                throw new Exception("The uniform value " + variableName + " is defined outside of a uniform block, which is not supported by the engine.");
            }

            var indexOfResource = effectReflection.ResourceBindings.FindIndex(x => x.RawName == constantBufferDescription.Name);
            if (indexOfResource == -1)
            {
                reflectionResult.Error($"Unable to find uniform [{uniformName}] in any constant buffer");
                return;
            }

            //var constantBufferDescription = effectReflection.ConstantBuffers[indexOfConstantBufferDescription];
            var constantBuffer = effectReflection.ResourceBindings[indexOfResource];

            // First time we encounter this cbuffer?
            if (!validConstantBuffers[indexOfConstantBuffer])
            {
                constantBuffer.SlotStart = ConstantBufferOffsets.Length - 1;

                // Find next cbuffer slot
                Array.Resize(ref ConstantBufferOffsets, ConstantBufferOffsets.Length + 1);

                effectReflection.ResourceBindings[indexOfResource] = constantBuffer;

                ConstantBufferOffsets[constantBuffer.SlotStart + 1] = ConstantBufferOffsets[constantBuffer.SlotStart] + constantBufferDescription.Size;

                validConstantBuffers[indexOfConstantBuffer] = true;
            }

            //var elementSize = uniformSize;

            // For array, each element is rounded to register size
            //if (uniformSize%16 != 0 && uniformCount > 1)
            //{
            //    constantBufferDescription.Size = (constantBufferDescription.Size + 15)/16*16;
            //    uniformSize = (uniformSize + 15)/16*16;
            //}

            // Check if it can fits in the same register, otherwise starts at the next one
            //if (uniformCount == 1 && constantBufferDescription.Size/16 != (constantBufferDescription.Size + uniformSize - 1)/16)
            //    constantBufferDescription.Size = (constantBufferDescription.Size + 15)/16*16;

            var variable = constantBufferDescription.Members[indexOfMember];

            // Resolve array/member
            var offset = variable.Offset;
            var type = variable.Type;

            for (int i = 0; i < uniformParts.Count; ++i)
            {
                var uniformPart = uniformParts[i];

                // Apply member
                if (i > 0)
                {
                    if (type.Members == null)
                        throw new InvalidOperationException($"Tried to find member \"{uniformName.Substring(uniformPart.Start, uniformPart.Count)}\" on a non-struct type when processing \"{uniformName}\"");

                    bool memberFound = false;
                    for (int memberIndex = 0; memberIndex < type.Members.Length; ++memberIndex)
                    {
                        var member = type.Members[memberIndex];
                        if (string.Compare(member.Name, 0, uniformName, uniformPart.Start, uniformPart.Count) == 0)
                        {
                            // Adjust offset and set new type
                            offset += member.Offset;
                            type = member.Type;

                            memberFound = true;
                            break;
                        }
                    }

                    if (!memberFound)
                        throw new InvalidOperationException($"Couldn't find member \"{uniformName.Substring(uniformPart.Start, uniformPart.Count)}\" on struct type \"{type.Name}\" when processing \"{uniformName}\"");
                }
                   
                // Apply indexer for arrays
                if (uniformPart.Indexer != -1)
                {
                    offset += (type.ElementSize + 15) / 16 * 16 * uniformPart.Indexer;
                }
            }

            // Check type
            if (type.Type != GetTypeFromActiveUniformType(uniformType))
                throw new InvalidOperationException($"Uniform [{uniformName}] of type [{variable.Type.Type}] doesn't match OpenGL shader expected type [{GetTypeFromActiveUniformType(uniformType)}]");

            // No need to compare last element padding.
            // TODO: In case of float1/float2 arrays (rare) it is quite non-optimal to do a CompareMemory
            //variable.Size = uniformSize * (uniformCount - 1) + elementSize;
            //constantBufferDescription.Members[indexOfUniform] = variable;

            Uniforms.Add(new Uniform
            {
                Type = uniformType,
                Count = uniformCount,
                CompareSize = uniformSize + (uniformSize + 15)/16*16 * (uniformCount - 1),
                ConstantBufferSlot = constantBuffer.SlotStart,
                Offset = offset,
                UniformIndex = GL.GetUniformLocation(ProgramId, uniformName)
            });
        }
#endif

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

        private static int GetCountFromActiveUniformType(ActiveUniformType type)
        {
            switch (type)
            {
                case ActiveUniformType.Int:
                case ActiveUniformType.Float:
                case ActiveUniformType.Bool:
                    return 1;
                case ActiveUniformType.IntVec2:
                case ActiveUniformType.UnsignedIntVec2:
                case ActiveUniformType.FloatVec2:
                case ActiveUniformType.BoolVec2:
                    return 2;
                case ActiveUniformType.IntVec3:
                case ActiveUniformType.UnsignedIntVec3:
                case ActiveUniformType.FloatVec3:
                case ActiveUniformType.BoolVec3:
                    return 3;
                case ActiveUniformType.IntVec4:
                case ActiveUniformType.UnsignedIntVec4:
                case ActiveUniformType.FloatVec4:
                case ActiveUniformType.BoolVec4:
                case ActiveUniformType.FloatMat2:
                    return 4;
                case ActiveUniformType.FloatMat2x3:
                case FloatMat3x2:
                    return 6;
                case ActiveUniformType.FloatMat2x4:
                case ActiveUniformType.FloatMat4x2:
                    return 8;
                case ActiveUniformType.FloatMat3:
                    return 9;
                case ActiveUniformType.FloatMat3x4:
                case ActiveUniformType.FloatMat4x3:
                    return 12;
                case ActiveUniformType.FloatMat4:
                    return 16;
                
                case ActiveUniformType.Sampler2D:
                case ActiveUniformType.SamplerCube:
                case ActiveUniformType.Sampler3D:
                case ActiveUniformType.Sampler2DShadow:
                case ActiveUniformType.SamplerCubeShadow:
                case ActiveUniformType.IntSampler2D:
                case ActiveUniformType.IntSampler3D:
                case ActiveUniformType.IntSamplerCube:
                case ActiveUniformType.UnsignedIntSampler2D:
                case ActiveUniformType.UnsignedIntSampler3D:
                case ActiveUniformType.UnsignedIntSamplerCube:
                case ActiveUniformType.Sampler2DArray:
                case ActiveUniformType.Sampler2DArrayShadow:
                case ActiveUniformType.IntSampler2DArray:
                case ActiveUniformType.UnsignedIntSampler2DArray:
#if !XENKO_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler1D:
                case ActiveUniformType.Sampler1DShadow:
                case ActiveUniformType.Sampler2DRect:
                case ActiveUniformType.Sampler2DRectShadow:
                case ActiveUniformType.IntSampler1D:
                case ActiveUniformType.IntSampler2DRect:
                case ActiveUniformType.UnsignedIntSampler1D:
                case ActiveUniformType.UnsignedIntSampler2DRect:
                case ActiveUniformType.Sampler1DArray:
                case ActiveUniformType.Sampler1DArrayShadow:
                case ActiveUniformType.IntSampler1DArray:
                case ActiveUniformType.UnsignedIntSampler1DArray:
#endif
                    return 1;
#if !XENKO_GRAPHICS_API_OPENGLES
                case ActiveUniformType.SamplerBuffer:
                case ActiveUniformType.IntSamplerBuffer:
                case ActiveUniformType.UnsignedIntSamplerBuffer:
                    return 1;
                case ActiveUniformType.Sampler2DMultisample:
                case ActiveUniformType.IntSampler2DMultisample:
                case ActiveUniformType.UnsignedIntSampler2DMultisample:
                    return 1;
                case ActiveUniformType.Sampler2DMultisampleArray:
                case ActiveUniformType.IntSampler2DMultisampleArray:
                case ActiveUniformType.UnsignedIntSampler2DMultisampleArray:
#endif
                    return 1;
                default:
                    //TODO: log error ?
                    return 0;
            }
        }

        private static EffectParameterClass GetClassFromActiveUniformType(ActiveUniformType type)
        {
            switch (type)
            {
                case ActiveUniformType.Int:
                case ActiveUniformType.Float:
                case ActiveUniformType.Bool:
                    return EffectParameterClass.Scalar;
                case ActiveUniformType.FloatVec2:
                case ActiveUniformType.FloatVec3:
                case ActiveUniformType.FloatVec4:
                case ActiveUniformType.IntVec2:
                case ActiveUniformType.IntVec3:
                case ActiveUniformType.IntVec4:
                case ActiveUniformType.BoolVec2:
                case ActiveUniformType.BoolVec3:
                case ActiveUniformType.BoolVec4:
                case ActiveUniformType.UnsignedIntVec2:
                case ActiveUniformType.UnsignedIntVec3:
                case ActiveUniformType.UnsignedIntVec4:
                    return EffectParameterClass.Vector;
                case ActiveUniformType.FloatMat2:
                case ActiveUniformType.FloatMat3:
                case ActiveUniformType.FloatMat4:
                case ActiveUniformType.FloatMat2x3:
                case ActiveUniformType.FloatMat2x4:
                case FloatMat3x2:
                case ActiveUniformType.FloatMat3x4:
                case ActiveUniformType.FloatMat4x2:
                case ActiveUniformType.FloatMat4x3:
                    return EffectParameterClass.MatrixColumns;
                    //return EffectParameterClass.MatrixRows;
                    //return EffectParameterClass.Vector;
                case ActiveUniformType.Sampler2D:
                case ActiveUniformType.SamplerCube:
                case ActiveUniformType.Sampler3D:
                case ActiveUniformType.Sampler2DShadow:
                case ActiveUniformType.Sampler2DArray:
                case ActiveUniformType.Sampler2DArrayShadow:
                case ActiveUniformType.SamplerCubeShadow:
                case ActiveUniformType.IntSampler2D:
                case ActiveUniformType.IntSampler3D:
                case ActiveUniformType.IntSamplerCube:
                case ActiveUniformType.IntSampler2DArray:
                case ActiveUniformType.UnsignedIntSampler2D:
                case ActiveUniformType.UnsignedIntSampler3D:
                case ActiveUniformType.UnsignedIntSamplerCube:
                case ActiveUniformType.UnsignedIntSampler2DArray:
#if !XENKO_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler1D:
                case ActiveUniformType.Sampler1DShadow:
                case ActiveUniformType.Sampler2DRect:
                case ActiveUniformType.Sampler2DRectShadow:
                case ActiveUniformType.Sampler1DArray:
                case ActiveUniformType.SamplerBuffer:
                case ActiveUniformType.Sampler1DArrayShadow:
                case ActiveUniformType.IntSampler1D:
                case ActiveUniformType.IntSampler2DRect:
                case ActiveUniformType.IntSampler1DArray:
                case ActiveUniformType.IntSamplerBuffer:
                case ActiveUniformType.UnsignedIntSampler1D:
                case ActiveUniformType.UnsignedIntSampler2DRect:
                case ActiveUniformType.UnsignedIntSampler1DArray:
                case ActiveUniformType.UnsignedIntSamplerBuffer:
                case ActiveUniformType.Sampler2DMultisample:
                case ActiveUniformType.IntSampler2DMultisample:
                case ActiveUniformType.UnsignedIntSampler2DMultisample:
                case ActiveUniformType.Sampler2DMultisampleArray:
                case ActiveUniformType.IntSampler2DMultisampleArray:
                case ActiveUniformType.UnsignedIntSampler2DMultisampleArray:
#endif
                    return EffectParameterClass.TextureBuffer;
                default:
                    //TODO: log error ?
                    return EffectParameterClass.Object;
            }
        }

        private static EffectParameterType GetTypeFromActiveUniformType(ActiveUniformType type)
        {
            switch (type)
            {
                case ActiveUniformType.Int:
                case ActiveUniformType.IntVec2:
                case ActiveUniformType.IntVec3:
                case ActiveUniformType.IntVec4:
                    return EffectParameterType.Int;
                case ActiveUniformType.Float:
                case ActiveUniformType.FloatVec2:
                case ActiveUniformType.FloatVec3:
                case ActiveUniformType.FloatVec4:
                case ActiveUniformType.FloatMat2:
                case ActiveUniformType.FloatMat3:
                case ActiveUniformType.FloatMat4:
                case ActiveUniformType.FloatMat2x3:
                case ActiveUniformType.FloatMat2x4:
                case FloatMat3x2:
                case ActiveUniformType.FloatMat3x4:
                case ActiveUniformType.FloatMat4x2:
                case ActiveUniformType.FloatMat4x3:
                    return EffectParameterType.Float;
                case ActiveUniformType.Bool:
                case ActiveUniformType.BoolVec2:
                case ActiveUniformType.BoolVec3:
                case ActiveUniformType.BoolVec4:
                    return EffectParameterType.Bool;
                case ActiveUniformType.UnsignedIntVec2:
                case ActiveUniformType.UnsignedIntVec3:
                case ActiveUniformType.UnsignedIntVec4:
                    return EffectParameterType.UInt;
#if !XENKO_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler1D:
                case ActiveUniformType.Sampler1DShadow:
                case ActiveUniformType.IntSampler1D:
                case ActiveUniformType.UnsignedIntSampler1D:
                    return EffectParameterType.Texture1D;
#endif
                case ActiveUniformType.Sampler2D:
                case ActiveUniformType.Sampler2DShadow:
                case ActiveUniformType.IntSampler2D:
                case ActiveUniformType.UnsignedIntSampler2D:
#if !XENKO_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler2DRect:
                case ActiveUniformType.Sampler2DRectShadow:
                case ActiveUniformType.IntSampler2DRect:
                case ActiveUniformType.UnsignedIntSampler2DRect:
#endif
                    return EffectParameterType.Texture2D;
                case ActiveUniformType.Sampler3D:
                case ActiveUniformType.IntSampler3D:
                case ActiveUniformType.UnsignedIntSampler3D:
                    return EffectParameterType.Texture3D;
                case ActiveUniformType.SamplerCube:
                case ActiveUniformType.SamplerCubeShadow:
                case ActiveUniformType.IntSamplerCube:
                case ActiveUniformType.UnsignedIntSamplerCube:
                    return EffectParameterType.TextureCube;
                case ActiveUniformType.Sampler2DArray:
                case ActiveUniformType.Sampler2DArrayShadow:
                case ActiveUniformType.IntSampler2DArray:
                case ActiveUniformType.UnsignedIntSampler2DArray:
                    return EffectParameterType.Texture2DArray;
#if !XENKO_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler1DArray:
                case ActiveUniformType.Sampler1DArrayShadow:
                case ActiveUniformType.IntSampler1DArray:
                case ActiveUniformType.UnsignedIntSampler1DArray:
                    return EffectParameterType.Texture1DArray;
                case ActiveUniformType.SamplerBuffer:
                case ActiveUniformType.IntSamplerBuffer:
                case ActiveUniformType.UnsignedIntSamplerBuffer:
                    return EffectParameterType.TextureBuffer;
                case ActiveUniformType.Sampler2DMultisample:
                case ActiveUniformType.IntSampler2DMultisample:
                case ActiveUniformType.UnsignedIntSampler2DMultisample:
                    return EffectParameterType.Texture2DMultisampled;
                case ActiveUniformType.Sampler2DMultisampleArray:
                case ActiveUniformType.IntSampler2DMultisampleArray:
                case ActiveUniformType.UnsignedIntSampler2DMultisampleArray:
                    return EffectParameterType.Texture2DMultisampledArray;
#endif
                default:
                    //TODO: log error ?
                    return EffectParameterType.Void;
            }
        }

        class UniformMergeInfo
        {
            public ActiveUniformType Type;
            public int Offset;
            public int NextOffset;
            public string Name;
        }
    }
}
#endif
