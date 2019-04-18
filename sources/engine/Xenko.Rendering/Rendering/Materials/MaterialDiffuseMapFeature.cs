// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Rendering.Materials.ComputeColors;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// A Diffuse map for the diffuse material feature.
    /// </summary>
    [DataContract("MaterialDiffuseMapFeature")]
    [Display("Diffuse Map")]
    public class MaterialDiffuseMapFeature : MaterialFeature, IMaterialDiffuseFeature, IMaterialStreamProvider
    {
        public static readonly MaterialStreamDescriptor DiffuseStream = new MaterialStreamDescriptor("Diffuse", "matDiffuse", MaterialKeys.DiffuseValue.PropertyType);
        public static readonly MaterialStreamDescriptor ColorBaseStream = new MaterialStreamDescriptor("Color base", "matColorBase", MaterialKeys.DiffuseValue.PropertyType);

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialDiffuseMapFeature"/> class.
        /// </summary>
        public MaterialDiffuseMapFeature()
        {
            DiffuseMap = new ComputeTextureColor();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialDiffuseMapFeature"/> class.
        /// </summary>
        /// <param name="diffuseMap">The diffuse map.</param>
        public MaterialDiffuseMapFeature(IComputeColor diffuseMap)
        {
            if (diffuseMap == null) throw new ArgumentNullException("diffuseMap");
            DiffuseMap = diffuseMap;
        }

        /// <summary>
        /// Gets or sets the diffuse map.
        /// </summary>
        /// <value>The diffuse map.</value>
        [Display("Diffuse Map")]
        [NotNull]
        [DataMemberCustomSerializer]
        public IComputeColor DiffuseMap { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            if (DiffuseMap != null)
            {
                Vector4 diffuseMin = Vector4.Zero;
                Vector4 diffuseMax = Vector4.One;
                DiffuseMap.ClampFloat4(ref diffuseMin, ref diffuseMax);

                var computeColorSource = DiffuseMap.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White));
                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(new ShaderClassSource("MaterialSurfaceDiffuse"));
                mixin.AddComposition("diffuseMap", computeColorSource);
                context.UseStream(MaterialShaderStage.Pixel, DiffuseStream.Stream);
                context.UseStream(MaterialShaderStage.Pixel, ColorBaseStream.Stream);
                context.AddShaderSource(MaterialShaderStage.Pixel, mixin);
            }
        }

        public IEnumerable<MaterialStreamDescriptor> GetStreams()
        {
            yield return ColorBaseStream;
            yield return DiffuseStream;
        }
    }
}
