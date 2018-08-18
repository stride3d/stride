// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
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
            context.MaterialPass.TessellationMethod |= XenkoTessellationMethod.PointNormal;

            // create and affect the shader source
            var tessellationShader = new ShaderMixinSource();
            tessellationShader.Mixins.Add(new ShaderClassSource("TessellationPN"));
            if (AdjacentEdgeAverage)
                tessellationShader.Mixins.Add(new ShaderClassSource("TessellationAE4", "PositionWS"));

            context.Parameters.Set(MaterialKeys.TessellationShader, tessellationShader);
        }
    }
}
