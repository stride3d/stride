// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Storage;
using Xenko.Rendering;
using Xenko.Shaders;
using Xenko.Shaders.Compiler;

namespace Xenko.Graphics
{
    [ContentSerializer(typeof(DataContentSerializer<Effect>))]
    [DataSerializer(typeof(EffectSerializer))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Effect>), Profile = "Content")]
    public class Effect : ComponentBase
    {
        private GraphicsDevice graphicsDeviceDefault;
        private EffectReflection reflection;

        private EffectBytecode bytecode;

        internal Effect()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Effect"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="bytecode">The bytecode.</param>
        /// <param name="usedParameters">The parameters used to create this shader (from a xkfx).</param>
        /// <exception cref="System.ArgumentNullException">
        /// device
        /// or
        /// bytecode
        /// </exception>
        public Effect(GraphicsDevice device, EffectBytecode bytecode)
        {
            InitializeFrom(device, bytecode);
        }

        internal void InitializeFrom(GraphicsDevice device, EffectBytecode bytecode)
        {
            if (device == null) throw new ArgumentNullException("device");
            if (bytecode == null) throw new ArgumentNullException("bytecode");

            this.graphicsDeviceDefault = device;
            this.bytecode = bytecode;
            Initialize();
        }

        /// <summary>
        /// Gets the bytecode.
        /// </summary>
        /// <value>The bytecode.</value>
        public EffectBytecode Bytecode
        {
            get
            {
                return bytecode;
            }
        }

        internal bool SourceChanged { get; set; }

        public bool HasParameter(ParameterKey parameterKey)
        {
            // Check resources
            for (int i = 0; i < reflection.ResourceBindings.Count; i++)
            {
                var key = reflection.ResourceBindings[i].KeyInfo.Key;
                if (key == parameterKey)
                    return true;
            }

            // Check cbuffer
            foreach (var constantBuffer in reflection.ConstantBuffers)
            {
                var constantBufferMembers = constantBuffer.Members;

                for (int i = 0; i < constantBufferMembers.Length; ++i)
                {
                    var key = constantBufferMembers[i].KeyInfo.Key;
                    if (key == parameterKey)
                        return true;
                }
            }

            return false;
        }

        private void Initialize()
        {
            reflection = bytecode.Reflection;

            PrepareReflection(reflection);
            LoadDefaultParameters();
        }

        private static void PrepareReflection(EffectReflection reflection)
        {
            // prepare resource bindings used internally
            for (int i = 0; i < reflection.ResourceBindings.Count; i++)
            {
                // it is fine if multiple threads do this at the same time (same result)
                // we use ref to avoid reassigning to the list (which cause a Collection modified during enumeration exception)
                UpdateResourceBindingKey(ref reflection.ResourceBindings.Items[i]);
            }
            foreach (var constantBuffer in reflection.ConstantBuffers)
            {
                var constantBufferMembers = constantBuffer.Members;

                for (int i = 0; i < constantBufferMembers.Length; ++i)
                {
                    // Update binding key
                    UpdateValueBindingKey(ref constantBufferMembers[i]);
                }
            }

            UpdateConstantBufferHashes(reflection);
        }

        private void LoadDefaultParameters()
        {
            // Create parameter bindings
            for (int i = 0; i < reflection.ResourceBindings.Count; i++)
            {
                // Update binding key
                var key = reflection.ResourceBindings[i].KeyInfo.Key;

                if (reflection.ResourceBindings[i].Class == EffectParameterClass.Sampler)
                {
                    var samplerBinding = reflection.SamplerStates.FirstOrDefault(x => x.KeyName == reflection.ResourceBindings[i].KeyInfo.KeyName);
                    if (samplerBinding != null)
                    {
                        samplerBinding.Key = key;
                        var samplerDescription = samplerBinding.Description;
                        // TODO GRAPHICS REFACTOR
                        //defaultParameters.Set((ObjectParameterKey<SamplerState>)key, SamplerState.New(graphicsDeviceDefault, samplerDescription));
                    }
                }
            }

            // Create constant buffers from descriptions (previously generated from shader reflection)
            foreach (var constantBuffer in reflection.ConstantBuffers)
            {
                // TODO GRAPHICS REFACTOR (check if necessary)
                // Handle ConstantBuffer. Share the same key ParameterConstantBuffer with all the stages
                //var parameterConstantBuffer = new ParameterConstantBuffer(graphicsDeviceDefault, constantBuffer.Name, constantBuffer);
                //var constantBufferKey = ParameterKeys.New<Buffer>(constantBuffer.Name);
                //shaderParameters.RegisterParameter(constantBufferKey, false);

                //for (int i = 0; i < resourceBindings.Length; i++)
                //{
                //    if (resourceBindings[i].Description.Param.Class == EffectParameterClass.ConstantBuffer && resourceBindings[i].Description.Param.Key.Name == constantBuffer.Name)
                //    {
                //        resourceBindings[i].Description.Param.Key = constantBufferKey;
                //    }
                //}
            }
        }

        private static void UpdateResourceBindingKey(ref EffectResourceBindingDescription binding)
        {
            var keyName = binding.KeyInfo.KeyName;

            switch (binding.Class)
            {
                case EffectParameterClass.Sampler:
                    binding.KeyInfo.Key = FindOrCreateResourceKey<SamplerState>(keyName);
                    break;
                case EffectParameterClass.ConstantBuffer:
                case EffectParameterClass.TextureBuffer:
                case EffectParameterClass.ShaderResourceView:
                case EffectParameterClass.UnorderedAccessView:
                    switch (binding.Type)
                    {
                        case EffectParameterType.Buffer:
                        case EffectParameterType.ConstantBuffer:
                        case EffectParameterType.TextureBuffer:
                        case EffectParameterType.AppendStructuredBuffer:
                        case EffectParameterType.ByteAddressBuffer:
                        case EffectParameterType.ConsumeStructuredBuffer:
                        case EffectParameterType.StructuredBuffer:
                        case EffectParameterType.RWBuffer:
                        case EffectParameterType.RWStructuredBuffer:
                        case EffectParameterType.RWByteAddressBuffer:
                            binding.KeyInfo.Key = FindOrCreateResourceKey<Buffer>(keyName);
                            break;
                        case EffectParameterType.Texture:
                        case EffectParameterType.Texture1D:
                        case EffectParameterType.Texture1DArray:
                        case EffectParameterType.RWTexture1D:
                        case EffectParameterType.RWTexture1DArray:
                        case EffectParameterType.Texture2D:
                        case EffectParameterType.Texture2DArray:
                        case EffectParameterType.Texture2DMultisampled:
                        case EffectParameterType.Texture2DMultisampledArray:
                        case EffectParameterType.RWTexture2D:
                        case EffectParameterType.RWTexture2DArray:
                        case EffectParameterType.TextureCube:
                        case EffectParameterType.TextureCubeArray:
                        case EffectParameterType.RWTexture3D:
                        case EffectParameterType.Texture3D:
                            binding.KeyInfo.Key = FindOrCreateResourceKey<Texture>(keyName);
                            break;
                    }
                    break;
            }

            if (binding.KeyInfo.Key == null)
            {
                throw new InvalidOperationException(string.Format("Unable to find/generate key [{0}] with unsupported type [{1}/{2}]", binding.KeyInfo.KeyName, binding.Class, binding.Type));
            }
        }

        private static void UpdateValueBindingKey(ref EffectValueDescription binding)
        {
            switch (binding.Type.Class)
            {
                case EffectParameterClass.Scalar:
                    switch (binding.Type.Type)
                    {
                        case EffectParameterType.Bool:
                            binding.KeyInfo.Key = FindOrCreateValueKey<bool>(binding);
                            break;
                        case EffectParameterType.Int:
                            binding.KeyInfo.Key = FindOrCreateValueKey<int>(binding);
                            break;
                        case EffectParameterType.UInt:
                            binding.KeyInfo.Key = FindOrCreateValueKey<uint>(binding);
                            break;
                        case EffectParameterType.Float:
                            binding.KeyInfo.Key = FindOrCreateValueKey<float>(binding);
                            break;
                    }
                    break;
                case EffectParameterClass.Color:
                    {
                        var componentCount = binding.Type.RowCount != 1 ? binding.Type.RowCount : binding.Type.ColumnCount;
                        switch (binding.Type.Type)
                        {
                            case EffectParameterType.Float:
                                binding.KeyInfo.Key = componentCount == 4
                                                        ? FindOrCreateValueKey<Color4>(binding)
                                                        : (componentCount == 3 ? FindOrCreateValueKey<Color3>(binding) : null);
                                break;
                        }
                    }
                    break;
                case EffectParameterClass.Vector:
                    {
                        var componentCount = binding.Type.RowCount != 1 ? binding.Type.RowCount : binding.Type.ColumnCount;
                        switch (binding.Type.Type)
                        {
                            case EffectParameterType.Bool:
                            case EffectParameterType.Int:
                                binding.KeyInfo.Key = componentCount == 4 ? (ParameterKey)FindOrCreateValueKey<Int4>(binding) : (componentCount == 3 ? FindOrCreateValueKey<Int3>(binding) : null);
                                break;
                            case EffectParameterType.UInt:
                                binding.KeyInfo.Key = componentCount == 4 ? FindOrCreateValueKey<UInt4>(binding) : null;
                                break;
                            case EffectParameterType.Float:
                                binding.KeyInfo.Key = componentCount == 4
                                                        ? FindOrCreateValueKey<Vector4>(binding)
                                                        : (componentCount == 3 ? (ParameterKey)FindOrCreateValueKey<Vector3>(binding) : (componentCount == 2 ? FindOrCreateValueKey<Vector2>(binding) : null));
                                break;
                        }
                    }
                    break;
                case EffectParameterClass.MatrixRows:
                case EffectParameterClass.MatrixColumns:
                    binding.KeyInfo.Key = FindOrCreateValueKey<Matrix>(binding);
                    break;
                case EffectParameterClass.Struct:
                    binding.KeyInfo.Key = ParameterKeys.FindByName(binding.KeyInfo.KeyName);
                    break;
            }

            if (binding.KeyInfo.Key == null)
            {
                throw new InvalidOperationException(string.Format("Unable to find/generate key [{0}] with unsupported type [{1}/{2}]", binding.KeyInfo.KeyName, binding.Type.Class, binding.Type.Type));
            }
        }

        private static ParameterKey FindOrCreateResourceKey<T>(string name)
        {
            return ParameterKeys.FindByName(name) ?? ParameterKeys.NewObject<T>(default(T), name);
        }

        private static ParameterKey FindOrCreateValueKey<T>(EffectValueDescription binding) where T : struct
        {
            var name = binding.KeyInfo.KeyName;
            return ParameterKeys.FindByName(name) ?? ParameterKeys.NewValue<T>(default(T), name);
        }

        private static void UpdateConstantBufferHashes(EffectReflection reflection)
        {
            // Update Constant buffers description
            foreach (var constantBuffer in reflection.ConstantBuffers)
            {
                // We will generate a unique hash that depends on cbuffer layout (to easily detect if they differ when binding a new effect)
                // TODO: currently done at runtime, but it should better be done at compile time
                var hashBuilder = new ObjectIdBuilder();
                hashBuilder.Write(constantBuffer.Name);
                hashBuilder.Write(constantBuffer.Size);

                for (int i = 0; i < constantBuffer.Members.Length; ++i)
                {
                    var member = constantBuffer.Members[i];
                    HashConstantBufferMember(ref hashBuilder, ref member);
                }

                // Update the hash
                constantBuffer.Hash = hashBuilder.ComputeHash();
            }
        }

        internal static void HashConstantBufferMember(ref ObjectIdBuilder hashBuilder, ref EffectValueDescription member, int baseOffset = 0)
        {
            hashBuilder.Write(member.KeyInfo.Key.Name);
            hashBuilder.Write(member.Offset - baseOffset);
            hashBuilder.Write(member.Size);

            HashType(ref hashBuilder, ref member.Type);
        }

        private static void HashType(ref ObjectIdBuilder hashBuilder, ref EffectTypeDescription type)
        {
            hashBuilder.Write(type.RowCount);
            hashBuilder.Write(type.ColumnCount);
            hashBuilder.Write(type.Elements);
            if (type.Name != null)
                hashBuilder.Write(type.Name);

            if (type.Members != null)
            {
                foreach (var member in type.Members)
                {
                    hashBuilder.Write(member.Name);
                    hashBuilder.Write(member.Offset);
                    var memberType = member.Type;
                    HashType(ref hashBuilder, ref memberType);
                }
            }
        }
    }
}
