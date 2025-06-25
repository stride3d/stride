// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   A description of a <strong>Blend State</strong> for a Render Target, which defines how colors are blended when rendering.
///   <br/>
/// </summary>
/// <remarks>
///   This structure controls transparency, color mixing, and blend modes for a Render Target. Modify this to achieve effects
///   like alpha blending, additive blending, or custom shader-based blends.
/// </remarks>
[DataContract]
public struct BlendStateRenderTargetDescription : IEquatable<BlendStateRenderTargetDescription>
{
    /// <summary>
    ///   A value indicating whether to enable or disable blending.
    /// </summary>
    public bool BlendEnable;

    /// <summary>
    ///   Specifies the first color (RGB) data source and includes an optional pre-blend operation.
    /// </summary>
    /// <seealso cref="Blend"/>
    public Blend ColorSourceBlend;

    /// <summary>
    ///   Specifies the second color (RGB) data source and includes an optional pre-blend operation.
    /// </summary>
    /// <seealso cref="Blend"/>
    public Blend ColorDestinationBlend;

    /// <summary>
    ///   Defines the function used to combine the color (RGB) data sources.
    /// </summary>
    /// <seealso cref="BlendFunction"/>
    public BlendFunction ColorBlendFunction;

    /// <summary>
    ///   Specifies the first alpha data source and includes an optional pre-blend operation.
    /// </summary>
    /// <seealso cref="Blend"/>
    /// <remarks>
    ///   <see cref="Blend"/> options that end in <c>Color</c> are not allowed.
    /// </remarks>
    public Blend AlphaSourceBlend;

    /// <summary>
    ///   Specifies the second alpha data source and includes an optional pre-blend operation.
    /// </summary>
    /// <seealso cref="Blend"/>
    /// <remarks>
    ///   <see cref="Blend"/> options that end in <c>Color</c> are not allowed.
    /// </remarks>
    public Blend AlphaDestinationBlend;

    /// <summary>
    ///   Defines the function used to combine the alpha data sources.
    /// </summary>
    /// <seealso cref="BlendFunction"/>
    public BlendFunction AlphaBlendFunction;

    /// <summary>
    ///   A combination of flags that specify which color channels (Red, Green, Blue, Alpha) can be written to the Render Target when blending.
    /// </summary>
    public ColorWriteChannels ColorWriteChannels;


    /// <inheritdoc/>
    public readonly bool Equals(BlendStateRenderTargetDescription other)
    {
        return BlendEnable == other.BlendEnable
            && ColorSourceBlend == other.ColorSourceBlend
            && ColorDestinationBlend == other.ColorDestinationBlend
            && ColorBlendFunction == other.ColorBlendFunction
            && AlphaSourceBlend == other.AlphaSourceBlend
            && AlphaDestinationBlend == other.AlphaDestinationBlend
            && AlphaBlendFunction == other.AlphaBlendFunction
            && ColorWriteChannels == other.ColorWriteChannels;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is BlendStateRenderTargetDescription description && Equals(description);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(BlendEnable, ColorSourceBlend, ColorDestinationBlend, ColorBlendFunction, AlphaSourceBlend, AlphaDestinationBlend, AlphaBlendFunction, ColorWriteChannels);
    }

    public static bool operator ==(BlendStateRenderTargetDescription left, BlendStateRenderTargetDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BlendStateRenderTargetDescription left, BlendStateRenderTargetDescription right)
    {
        return !left.Equals(right);
    }
}
