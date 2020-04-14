// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Helper class to build the <see cref="ShaderSource"/> for the shading model of a <see cref="IMaterialShadingModelFeature"/>.
    /// </summary>
    public class ShadingModelShaderBuilder
    {
        public List<ShaderSource> ShaderSources { get; } = new List<ShaderSource>();

        /// <summary>
        /// Shaders that needs to be mixed on top of MaterialSurfaceLightingAndShading.
        /// </summary>
        public List<ShaderClassSource> LightDependentExtraModels { get; } = new List<ShaderClassSource>();

        public ShaderSource LightDependentSurface { get; set; }
    }
}
