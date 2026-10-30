// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;

namespace Stride.Rendering.Materials.ComputeColors;

[DataContract(Inherited = true)]
[Display("Lerp Operator")]
[InlineProperty]
public class ComputeClampedLerpBase<T> : ComputeNode where T : class, IComputeNode
{
    [MaybeNull] public required T Black { get; set; }

    [MaybeNull] public required T White { get; set; }

    [MaybeNull] public required T Blend { get; set; }

    /// <inheritdoc/>
    public override IEnumerable<IComputeNode> GetChildren([MaybeNull] object context = null)
    {
        if (Black != null)
            yield return Black;
        if (White != null)
            yield return White;
        if (Blend != null)
            yield return Blend;
    }

    private const string BlackCompositionName = "color1";
    private const string WhiteCompositionName = "color2";
    private const string BlendCompositionName = "color3";

    /// <inheritdoc/>
    public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
    {
        var blackShaderSource = Black?.GenerateShaderSource(context, baseKeys);
        var whiteShaderSource = White?.GenerateShaderSource(context, baseKeys);
        var blendSharedSource = Blend?.GenerateShaderSource(context, baseKeys);

        var shaderSource = new ShaderClassSource("ComputeColorClampedLerp");
        var mixin = new ShaderMixinSource();
        mixin.Mixins.Add(shaderSource);
        if (blackShaderSource != null)
            mixin.AddComposition(BlackCompositionName, blackShaderSource);
        if (whiteShaderSource != null)
            mixin.AddComposition(WhiteCompositionName, whiteShaderSource);
        if (blendSharedSource != null)
            mixin.AddComposition(BlendCompositionName, blendSharedSource);

        return mixin;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "Lerp operation";
    }
}
