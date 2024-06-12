// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    [DataContract("MaterialShaderFeature")]
    [Display("Shader")]
    public class MaterialShaderFeature : MaterialFeature, IMaterialShaderFeature
    {
        private static readonly MaterialStreamDescriptor ShaderVertexStream = new MaterialStreamDescriptor("Shader", "matVertexShader", MaterialKeys.VertexStageStreamInitializer.PropertyType);
        //private static readonly MaterialStreamDescriptor ShaderPixelStream = new MaterialStreamDescriptor("Shader", "matPixelShader", MaterialKeys.PixelStageStreamInitializer.PropertyType);

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEmissiveMapFeature"/> class.
        /// </summary>
        public MaterialShaderFeature() : this(new ComputeShaderClassColor())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEmissiveMapFeature"/> class.
        /// </summary>
        /// <param name="emissiveMap">The emissive map.</param>
        /// <exception cref="System.ArgumentNullException">emissiveMap</exception>
        public MaterialShaderFeature(ComputeShaderClassColor shader)
        {
            if (shader == null) throw new ArgumentNullException("shader");
            Shader = shader;
        }

        /// <summary>
        /// Gets or sets the shader.
        /// </summary>
        /// <value>The diffuse map.</value>
        /// <userdoc>The map specifying the color emitted by the material.</userdoc>
        [Display("Shader")]
        [NotNull]
        [DataMember(10)]
        public ComputeShaderClassColor Shader { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            if (Shader != null && Shader.MixinReference != null && !Shader.MixinReference.Equals(""))
            {
                var shaderSource = Shader.GenerateShaderSource(context, null);
                context.AddCustomShaderSource(shaderSource);
            }
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return other is MaterialShaderFeature;
        }

        public bool IsValid()
        {
            return Shader != null;
        }
    }
}
