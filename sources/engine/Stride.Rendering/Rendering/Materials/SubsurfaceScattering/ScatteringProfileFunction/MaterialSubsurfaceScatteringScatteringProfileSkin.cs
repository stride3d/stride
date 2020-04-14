// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Calculates the scattering profile for skin, which is applied during
    /// the forward pass using the subsurface scattering shading model.
    /// It also calculates a scattering kernel based on the "Falloff" and "Strength" parameters.
    /// 
    /// </summary>
    [Display("Skin")]
    [DataContract("MaterialSubsurfaceScatteringScatteringProfileSkin")]
    public class MaterialSubsurfaceScatteringScatteringProfileSkin : IMaterialSubsurfaceScatteringScatteringProfile
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialSubsurfaceScatteringScatteringProfileSkin");
        }
    }
}
