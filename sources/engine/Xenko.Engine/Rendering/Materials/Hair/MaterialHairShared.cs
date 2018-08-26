// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    public struct SurfaceData // TODO: This structure is just a temporary workaround because the MaterialSurfaceShadingSpecularHair shader keeps generating a variable of this type, which is only defined inside the shader.
    {
    }

    public class HairShared
    {
        public enum HairShadingModel // These values must correspond to the ones defined in "MaterialHairShared.xksl".
        {
            [Display("Scheuermann approximation")]
            ScheuermannApproximation = 0,

            [Display("Scheuermann improved")]
            ScheuermannImproved = 1,

            [Display("Kajiya-Kay shifted")]
            KajiyaKayShifted = 2,
        }

        private enum PassID
        {
            OpaqueBackAndFront = 0,
            TransparentBack = 1,
            TransparentFront = 2,
        }

        private static readonly ValueParameterKey<float> HairAlphaThresholdKey = ParameterKeys.NewValue<float>();

        public static void SetMaterialPassParameters(MaterialGeneratorContext context, ShaderMixinSource shaderSource, float alphaThreshold)
        {
            const string functionName = "hairDiscardFunction";

            // Generate a key because the mixin can be used more than once per shader and therefore we want their constants to be independent.
            var uniqueAlphaThresholdKey = (ValueParameterKey<float>)context.GetParameterKey(HairAlphaThresholdKey);

            context.Parameters.Set(uniqueAlphaThresholdKey, alphaThreshold);

            switch (context.PassIndex)
            {
            case (int)PassID.OpaqueBackAndFront:
                context.MaterialPass.BlendState = BlendStates.Opaque;   // Render as opaque.
                context.MaterialPass.HasTransparency = false;   // Render as opaque.
                context.MaterialPass.CullMode = CullMode.None; // Render both faces
                shaderSource.AddComposition(functionName, new MaterialHairDiscardFunctionOpaquePass().Generate(context, uniqueAlphaThresholdKey));
                break;

            case (int)PassID.TransparentBack:
                context.MaterialPass.BlendState = BlendStates.NonPremultiplied; // TODO: Is this the correct blend mode?
                context.MaterialPass.HasTransparency = true;   // Render as a transparent.
                context.MaterialPass.CullMode = CullMode.Front;   // Only draw back faces.
                shaderSource.AddComposition(functionName, new MaterialHairDiscardFunctionTransparentPass().Generate(context, uniqueAlphaThresholdKey));
                break;

            case (int)PassID.TransparentFront:
                context.MaterialPass.BlendState = BlendStates.NonPremultiplied; // TODO: Is this the correct blend mode?
                context.MaterialPass.HasTransparency = true;   // Render as a transparent.
                context.MaterialPass.CullMode = CullMode.Back;   // Only draw front faces.
                shaderSource.AddComposition(functionName, new MaterialHairDiscardFunctionTransparentPass().Generate(context, uniqueAlphaThresholdKey));
                break;
            }
        }
    }
}
