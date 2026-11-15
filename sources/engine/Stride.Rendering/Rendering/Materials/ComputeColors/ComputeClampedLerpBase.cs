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
public abstract class ComputeClampedLerpBase<T> : ComputeNode where T : class, IComputeNode
{
    /// <summary>
    /// The color that would be displayed when <see cref="Blend"/> is entirely black (equal or less than zero),
    /// otherwise a blend of this and <see cref="White"/>
    /// </summary>
    public required T Black { get; set; }

    /// <summary>
    /// The color that would be displayed when <see cref="Blend"/> is completely white (equal or greater than one),
    /// otherwise a blend of this and <see cref="Black"/>
    /// </summary>
    public required T White { get; set; }

    /// <summary>
    /// The color used to blend between <see cref="Black"/> and <see cref="White"/>;
    /// a value of 0.5 would display a color halfway between them,
    /// a value closer to zero would tend towards <see cref="Black"/> while a value closer to one would tend towards <see cref="White"/>
    /// </summary>
    public required T Blend { get; set; }

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
        var blendShaderSource = Blend?.GenerateShaderSource(context, baseKeys);

        var shaderSource = new ShaderClassSource("ComputeColorClampedLerp");
        var mixin = new ShaderMixinSource();
        mixin.Mixins.Add(shaderSource);
        if (blackShaderSource != null)
            mixin.AddComposition(BlackCompositionName, blackShaderSource);
        if (whiteShaderSource != null)
            mixin.AddComposition(WhiteCompositionName, whiteShaderSource);
        if (blendShaderSource != null)
            mixin.AddComposition(BlendCompositionName, blendShaderSource);

        return mixin;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "Lerp operation";
    }
}
