// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Default Cel Shading ramp function applied
    /// </summary>
    [DataContract("MaterialCelShadingLightDefault")]
    [Display("Default")]
    public class MaterialCelShadingLightDefault : IMaterialCelShadingLightFunction
    {
        [DataMember(5)]
        [Display("Black and White")]
        public bool IsBlackAndWhite { get; set; } = false;

        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialCelShadingLightDefault", IsBlackAndWhite);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialCelShadingLightDefault;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialCelShadingLightDefault).GetHashCode();
        }
    }
}
