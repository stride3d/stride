// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Material for Point-Normal tessellation.
    /// </summary>
    [DataContract("MaterialTessellationPNFeature")]
    [Display("Point Normal Tessellation")]
    public class MaterialTessellationPNFeature : MaterialTessellationBaseFeature
    {
        public override void GenerateShader(MaterialGeneratorContext context)
        {
            base.GenerateShader(context);

            if (hasAlreadyTessellationFeature) 
                return;

            // set the tessellation method used enumeration
            context.MaterialPass.TessellationMethod |= StrideTessellationMethod.PointNormal;

            // create and affect the shader source
            var tessellationShader = new ShaderMixinSource();
            tessellationShader.Mixins.Add(new ShaderClassSource("TessellationPN"));
            if (AdjacentEdgeAverage)
                tessellationShader.Mixins.Add(new ShaderClassSource("TessellationAE4", "PositionWS"));

            context.Parameters.Set(MaterialKeys.TessellationShader, tessellationShader);
        }
    }
}
