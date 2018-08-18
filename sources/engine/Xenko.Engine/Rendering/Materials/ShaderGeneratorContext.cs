// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics;
using Xenko.Graphics.Data;
using Xenko.Rendering.Materials.ComputeColors;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Base class for generating shader class source with associated parameters.
    /// </summary>
    public class ShaderGeneratorContext : ComponentBase
    {
        // TODO: Document this class

        private readonly Dictionary<ParameterKey, int> parameterKeyIndices;

        private readonly Dictionary<SamplerStateDescription, ObjectParameterKey<SamplerState>> declaredSamplerStates;

        private readonly Dictionary<Color4, Texture> singleColorTextures = new Dictionary<Color4, Texture>();

        private MaterialOverrides currentOverrides;
        private Stack<MaterialOverrides> overridesStack = new Stack<MaterialOverrides>();

        public delegate object FindAssetDelegate(object asset);

        public delegate string GetAssetFriendlyNameDelegate(object asset);
        
        public FindAssetDelegate FindAsset { get; set; }

        public GetAssetFriendlyNameDelegate GetAssetFriendlyName { get; set; }

        public LoggerResult Log { get; set; }

        private GraphicsDevice graphicsDevice;

        /// <summary>
        /// Gets or sets the asset manager.
        /// </summary>
        /// <value>
        /// The asset manager.
        /// </value>
        public ContentManager Content { get; set; }

        public ShaderGeneratorContext(GraphicsDevice graphicsDevice = null)
        {
            this.graphicsDevice = graphicsDevice;
            Parameters = new ParameterCollection();
            parameterKeyIndices = new Dictionary<ParameterKey, int>();
            declaredSamplerStates = new Dictionary<SamplerStateDescription, ObjectParameterKey<SamplerState>>();
            currentOverrides = new MaterialOverrides();
        }

        public ParameterCollection Parameters { get; set; }

        public MaterialOverrides CurrentOverrides
        {
            get
            {
                return currentOverrides;
            }
        }

        public ColorSpace ColorSpace { get; set; }
        public bool IsNotPixelStage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether materials will be optimized (textures blended together, generate optimized shader permutations, etc...).
        /// </summary>
        /// <value>
        ///   <c>true</c> if [materials are optimized]; otherwise, <c>false</c>.
        /// </value>
        public bool OptimizeMaterials { get; set; }

        public ParameterKey GetParameterKey(ParameterKey key)
        {
            if (key == null) throw new ArgumentNullException("key");

            var baseKey = key;
            int parameterKeyIndex;
            parameterKeyIndices.TryGetValue(baseKey, out parameterKeyIndex);

            key = parameterKeyIndex == 0 ? baseKey : baseKey.ComposeWith("i" + parameterKeyIndex.ToString(CultureInfo.InvariantCulture));

            parameterKeyIndex++;
            parameterKeyIndices[baseKey] = parameterKeyIndex;
            return key;
        }

        public unsafe Texture GenerateTextureFromColor(Color color)
        {
            if (Content == null)
            {
                Log.Error("Trying to generate a texture without an ContentManager");
                return null;
            }

            // Already generated?
            Texture texture;
            if (singleColorTextures.TryGetValue(color, out texture))
                return texture;

            // Generate 1x1 texture of given color
            var image = Image.New2D(1, 1, 1, PixelFormat.R8G8B8A8_UNorm);
            *((Color*)image.PixelBuffer[0].DataPointer) = color;

            texture = image.ToSerializableVersion();

            // Save texture
            Content.Save(string.Format("__material_internal__/color_texture_{0:X2}{1:X2}{2:X2}{3:X2}", color.R, color.G, color.B, color.A), texture);

            singleColorTextures.Add(color, texture);

            return texture;
        }

        public ObjectParameterKey<Texture> GetTextureKey(Texture texture, ObjectParameterKey<Texture> key, Color? defaultTextureValue = null)
        {
            var textureKey = (ObjectParameterKey<Texture>)GetParameterKey(key);
            if (texture != null)
            {
                Parameters.Set(textureKey, texture);
            }
            else if (defaultTextureValue != null && Content != null)
            {
                texture = GenerateTextureFromColor(defaultTextureValue.Value);
                Parameters.Set(textureKey, texture);
            }
            return textureKey;
        }

        public ObjectParameterKey<SamplerState> GetSamplerKey(SamplerStateDescription samplerStateDesc, GraphicsDevice graphicsDevice)
        {
            ObjectParameterKey<SamplerState> key;

            if (!declaredSamplerStates.TryGetValue(samplerStateDesc, out key))
            {
                key = MaterialKeys.Sampler.ComposeWith("i" + declaredSamplerStates.Count.ToString(CultureInfo.InvariantCulture));
                declaredSamplerStates.Add(samplerStateDesc, key);
            }

            var samplerState = graphicsDevice != null ? SamplerState.New(graphicsDevice, samplerStateDesc) : SamplerState.NewFake(samplerStateDesc);
            Parameters.Set(key, samplerState);
            return key;
        }

        public ObjectParameterKey<Texture> GetTextureKey(ComputeTextureBase computeTexture, MaterialComputeColorKeys baseKeys)
        {
            var keyResolved = (ObjectParameterKey<Texture>)(computeTexture.Key ?? baseKeys.TextureBaseKey ?? MaterialKeys.GenericTexture);
            return GetTextureKey(computeTexture.Texture, keyResolved, baseKeys.DefaultTextureValue);
        }

        public ObjectParameterKey<SamplerState> GetSamplerKey(ComputeColorParameterSampler sampler)
        {
            if (sampler == null) throw new ArgumentNullException("sampler");

            var samplerStateDesc = new SamplerStateDescription(sampler.Filtering, sampler.AddressModeU)
            {
                AddressV = sampler.AddressModeV,
                AddressW = TextureAddressMode.Wrap,
            };
            return GetSamplerKey(samplerStateDesc, graphicsDevice);
        }

        public void PushOverrides(MaterialOverrides overrides)
        {
            if (overrides == null) throw new ArgumentNullException("overrides");
            overridesStack.Push(overrides);
            UpdateOverrides();
        }

        public void PopOverrides()
        {
            overridesStack.Pop();
            UpdateOverrides();
        }

        private void UpdateOverrides()
        {
            // Update overrides by squashing them using multiplication
            currentOverrides = new MaterialOverrides();
            foreach (var current in overridesStack)
            {
                currentOverrides *= current;
            }
        }
    }
}
