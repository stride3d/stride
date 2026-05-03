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
    #region Default values

    /// <summary>
    ///   Default value for <see cref="BlendEnable"/>.
    /// </summary>
    public const bool DefaultBlendEnable = false;

    /// <summary>
    ///   Default value for <see cref="ColorSourceBlend"/>.
    /// </summary>
    public const Blend DefaultColorSourceBlend = Blend.One;
    /// <summary>
    ///   Default value for <see cref="ColorDestinationBlend"/>.
    /// </summary>
    public const Blend DefaultColorDestinationBlend = Blend.Zero;
    /// <summary>
    ///   Default value for <see cref="ColorBlendFunction"/>.
    /// </summary>
    public const BlendFunction DefaultColorBlendFunction = BlendFunction.Add;

    /// <summary>
    ///   Default value for <see cref="AlphaSourceBlend"/>.
    /// </summary>
    public const Blend DefaultAlphaSourceBlend = Blend.One;
    /// <summary>
    ///   Default value for <see cref="AlphaDestinationBlend"/>.
    /// </summary>
    public const Blend DefaultAlphaDestinationBlend = Blend.Zero;
    /// <summary>
    ///   Default value for <see cref="AlphaBlendFunction"/>.
    /// </summary>
    public const BlendFunction DefaultAlphaBlendFunction = BlendFunction.Add;

    /// <summary>
    ///   Default value for <see cref="ColorWriteChannels"/>.
    /// </summary>
    public const ColorWriteChannels DefaultColorWriteChannels = ColorWriteChannels.All;

    #endregion

    /// <summary>
    ///   A value indicating whether to enable or disable blending.
    /// </summary>
    public bool BlendEnable = DefaultBlendEnable;

    /// <summary>
    ///   Specifies the first color (RGB) data source and includes an optional pre-blend operation.
    /// </summary>
    /// <seealso cref="Blend"/>
    public Blend ColorSourceBlend = DefaultColorSourceBlend;

    /// <summary>
    ///   Specifies the second color (RGB) data source and includes an optional pre-blend operation.
    /// </summary>
    /// <seealso cref="Blend"/>
    public Blend ColorDestinationBlend = DefaultColorDestinationBlend;

    /// <summary>
    ///   Defines the function used to combine the color (RGB) data sources.
    /// </summary>
    /// <seealso cref="BlendFunction"/>
    public BlendFunction ColorBlendFunction = DefaultColorBlendFunction;

    /// <summary>
    ///   Specifies the first alpha data source and includes an optional pre-blend operation.
    /// </summary>
    /// <seealso cref="Blend"/>
    /// <remarks>
    ///   <see cref="Blend"/> options that end in <c>Color</c> are not allowed.
    /// </remarks>
    public Blend AlphaSourceBlend = DefaultAlphaSourceBlend;

    /// <summary>
    ///   Specifies the second alpha data source and includes an optional pre-blend operation.
    /// </summary>
    /// <seealso cref="Blend"/>
    /// <remarks>
    ///   <see cref="Blend"/> options that end in <c>Color</c> are not allowed.
    /// </remarks>
    public Blend AlphaDestinationBlend = DefaultAlphaDestinationBlend;

    /// <summary>
    ///   Defines the function used to combine the alpha data sources.
    /// </summary>
    /// <seealso cref="BlendFunction"/>
    public BlendFunction AlphaBlendFunction = DefaultAlphaBlendFunction;

    /// <summary>
    ///   A combination of flags that specify which color channels (Red, Green, Blue, Alpha) can be written to the Render Target when blending.
    /// </summary>
    public ColorWriteChannels ColorWriteChannels = DefaultColorWriteChannels;


    /// <summary>
    ///   Initializes a new instance of the <see cref="BlendStateRenderTargetDescription"/> structure
    ///   with default values.
    /// </summary>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public BlendStateRenderTargetDescription()
    {
    }

    /// <summary>
    ///   A <see cref="BlendStateRenderTargetDescription"/> structure with default values.
    /// </summary>
    /// <remarks>
    ///   The default values are:
    ///   <list type="bullet">
    ///     <item>The blending is disabled.</item>
    ///     <item>
    ///       For both Color and Alpha:
    ///       <list type="bullet">
    ///         <item>
    ///           <term>Source</term>
    ///           <description><see cref="Blend.One"/></description>
    ///         </item>
    ///         <item>
    ///           <term>Destination</term>
    ///           <description><see cref="Blend.Zero"/></description>
    ///         </item>
    ///         <item>
    ///           <term>Compare Function</term>
    ///           <description><see cref="BlendFunction.Add"/> (additive)</description>
    ///         </item>
    ///       </list>
    ///     </item>
    ///     <item>All color channels can be written (<see cref="ColorWriteChannels.All"/>).</item>
    ///   </list>
    /// </remarks>
    public static readonly BlendStateRenderTargetDescription Default = new();


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
