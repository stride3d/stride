// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;

namespace Stride.Rendering.Materials.ComputeColors;

[DataContract("ComputeColorStackedNormals")]
[Display("Stacked Normals")]
[InlineProperty]
public class ComputeColorStackedNormals : ComputeNode, IComputeColor
{
    private bool cachedBaseIsXY, cachedDetailIsXY;

    /// <summary>
    /// The normal map that defines the base orientation of the normals
    /// </summary>
    public required IComputeColor Base { get; set; } = new ComputeTextureColor
    {
        FallbackValue = new ComputeColor(MaterialNormalMapFeature.DefaultNormalColor)
    };

    /// <summary>
    /// Gets or sets a value indicating whether the normal is only stored in XY components and Z is assumed to be sqrt(1 - x*x - y*y).
    /// </summary>
    /// <value><c>true</c> if this instance is xy normal; otherwise, <c>false</c>.</value>
    /// <userdoc>
    /// If there's no Z component in the texture, reconstruct it from the X and Y components. This assumes that Z = sqrt(1 - x*x - y*y) and that Z is always positive, so no normal vector can point to the back side of the surface. We recommend you enable this option, as Stride might remove the Z component when you compress normal maps.
    /// </userdoc>
    [DefaultValue(true)]
    [Display("Reconstruct Z")]
    public bool BaseIsXYNormal { get; set; } = true;

    /// <summary>
    /// The normal map laying on top of <see cref="Base"/>, normals are rotated to align with it
    /// </summary>
    public required IComputeColor Detail { get; set; } = new ComputeTextureColor
    {
        FallbackValue = new ComputeColor(MaterialNormalMapFeature.DefaultNormalColor)
    };

    /// <summary>
    /// Gets or sets a value indicating whether the normal is only stored in XY components and Z is assumed to be sqrt(1 - x*x - y*y).
    /// </summary>
    /// <value><c>true</c> if this instance is xy normal; otherwise, <c>false</c>.</value>
    /// <userdoc>
    /// If there's no Z component in the texture, reconstruct it from the X and Y components. This assumes that Z = sqrt(1 - x*x - y*y) and that Z is always positive, so no normal vector can point to the back side of the surface. We recommend you enable this option, as Stride might remove the Z component when you compress normal maps.
    /// </userdoc>
    [DefaultValue(true)]
    [Display("Reconstruct Z for Detail")]
    public bool DetailIsXYNormal { get; set; } = true;

    /// <inheritdoc/>
    public override IEnumerable<IComputeNode> GetChildren([MaybeNull] object context = null)
    {
        if (Base != null)
            yield return Base;
        if (Detail != null)
            yield return Detail;
    }

    private const string BaseCompositionName = "color1";
    private const string DetailCompositionName = "color2";

    /// <inheritdoc/>
    public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
    {
        var baseShaderSource = Base?.GenerateShaderSource(context, baseKeys);
        var detailShaderSource = Detail?.GenerateShaderSource(context, baseKeys);

        var shaderSource = new ShaderClassSource("ComputeColorStackedNormals", BaseIsXYNormal, DetailIsXYNormal);
        var mixin = new ShaderMixinSource();
        mixin.Mixins.Add(shaderSource);
        if (baseShaderSource != null)
            mixin.AddComposition(BaseCompositionName, baseShaderSource);
        if (detailShaderSource != null)
            mixin.AddComposition(DetailCompositionName, detailShaderSource);

        return mixin;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "Stacked normals";
    }

    public bool HasChanged
    {
        get
        {
            // Null children force skip changes
            if (Detail == null || Base == null)
                return false;

            if (cachedBaseIsXY != BaseIsXYNormal || cachedDetailIsXY != DetailIsXYNormal)
            {
                cachedBaseIsXY = BaseIsXYNormal;
                cachedDetailIsXY = DetailIsXYNormal;
                return true;
            }

            return Detail.HasChanged || Base.HasChanged;
        }
    }
}
