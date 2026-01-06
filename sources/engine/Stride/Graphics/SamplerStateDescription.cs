// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics;

/// <summary>
///   Describes a <strong>Sampler State</strong> object, which determines how to sample Texture data.
/// </summary>
/// <seealso cref="SamplerState"/>
[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct SamplerStateDescription : IEquatable<SamplerStateDescription>
{
    #region Default values

    /// <summary>
    ///   Default value for <see cref="Filter"/>.
    /// </summary>
    public const TextureFilter DefaultFilter = TextureFilter.Linear;
    /// <summary>
    ///   Default value for <see cref="AddressU"/>.
    /// </summary>
    public const TextureAddressMode DefaultAddressU = TextureAddressMode.Clamp;
    /// <summary>
    ///   Default value for <see cref="AddressV"/>.
    /// </summary>
    public const TextureAddressMode DefaultAddressV = TextureAddressMode.Clamp;
    /// <summary>
    ///   Default value for <see cref="AddressW"/>.
    /// </summary>
    public const TextureAddressMode DefaultAddressW = TextureAddressMode.Clamp;

    /// <summary>
    ///   Default value for <see cref="BorderColor"/> (black).
    /// </summary>
    public static readonly Color4 DefaultBorderColor = default; // Black (0,0,0,0)

    /// <summary>
    ///   Default value for <see cref="MaxAnisotropy"/>.
    /// </summary>
    public const int DefaultMaxAnisotropy = 16;
    /// <summary>
    ///   Default value for <see cref="MinMipLevel"/>.
    /// </summary>
    public const float DefaultMinMipLevel = -float.MaxValue;
    /// <summary>
    ///   Default value for <see cref="MaxMipLevel"/>.
    /// </summary>
    public const float DefaultMaxMipLevel = float.MaxValue;
    /// <summary>
    ///   Default value for <see cref="MipMapLevelOfDetailBias"/>.
    /// </summary>
    public const float DefaultMipMapLevelOfDetailBias = 0.0f;

    /// <summary>
    ///   Default value for <see cref="CompareFunction"/>.
    /// </summary>
    public const CompareFunction DefaultCompareFunction = CompareFunction.Never;

    #endregion

    /// <summary>
    ///   Initializes a new instance of the <see cref="SamplerStateDescription"/> structure
    ///   with default values.
    /// </summary>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public SamplerStateDescription()
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="SamplerStateDescription"/> structure
    ///   with default values, and a specific Texture filtering and addressing mode.
    /// </summary>
    /// <param name="filter">The Texture filtering mode.</param>
    /// <param name="addressMode">The Texture addressing mode for U, V, and W coordinates.</param>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public SamplerStateDescription(TextureFilter filter, TextureAddressMode addressMode) : this()
    {
        Filter = filter;
        AddressU = AddressV = AddressW = addressMode;
    }


    /// <summary>
    ///   The filtering method to use when sampling a Texture.
    /// </summary>
    public TextureFilter Filter = DefaultFilter;

    /// <summary>
    ///   The method to use for resolving a U texture coordinate that is outside the [0, 1] range.
    /// </summary>
    public TextureAddressMode AddressU = DefaultAddressU;

    /// <summary>
    ///   The method to use for resolving a V texture coordinate that is outside the [0, 1] range.
    /// </summary>
    public TextureAddressMode AddressV = DefaultAddressV;

    /// <summary>
    ///   The method to use for resolving a W texture coordinate that is outside the [0, 1] range.
    /// </summary>
    public TextureAddressMode AddressW = DefaultAddressW;

    /// <summary>
    ///   The offset to apply from the calculated mipmap level.
    /// </summary>
    /// <remarks>
    ///   For example, if a Texture should be sampled at mipmap level 3 and <see cref="MipMapLevelOfDetailBias"/>
    ///   is 2, then the Texture will be sampled at mipmap level 5.
    /// </remarks>
    public float MipMapLevelOfDetailBias = DefaultMipMapLevelOfDetailBias;

    /// <summary>
    ///   The clamping value used if <see cref="TextureFilter.Anisotropic"/> or <see cref="TextureFilter.ComparisonAnisotropic"/>
    ///   is specified in <see cref="Filter"/>. Valid values are between 1 and 16.
    /// </summary>
    public int MaxAnisotropy = DefaultMaxAnisotropy;

    /// <summary>
    ///   A function that compares sampled data against existing sampled data.
    /// </summary>
    /// <remarks>
    ///   This function will be used when specifying one of the comparison filtering modes in
    ///   <see cref="Filter"/>.
    /// </remarks>
    public CompareFunction CompareFunction = DefaultCompareFunction;

    /// <summary>
    ///   The border color to use if <see cref="TextureAddressMode.Border"/> is specified for
    ///   <see cref="AddressU"/>, <see cref="AddressV"/>, or <see cref="AddressW"/>.
    /// </summary>
    public Color4 BorderColor = DefaultBorderColor;

    /// <summary>
    ///   The lower end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap
    ///   level and any level higher than that is less detailed.
    /// </summary>
    public float MinMipLevel = DefaultMinMipLevel;

    /// <summary>
    ///   The upper end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap
    ///   level and any level higher than that is less detailed.
    /// </summary>
    /// <remarks>
    ///   This value must be greater than or equal to <see cref="MinMipLevel"/>.
    ///   To have no upper limit set this to a large value such as <see cref="float.MaxValue"/>.
    /// </remarks>
    public float MaxMipLevel = DefaultMaxMipLevel;


    /// <summary>
    ///   Returns a <see cref="SamplerStateDescription"/> with default values.
    /// </summary>
    /// <remarks>
    ///   The default values are:
    ///   <list type="bullet">
    ///     <item>Linear filtering (<see cref="TextureFilter.Linear"/>).</item>
    ///     <item><see cref="TextureAddressMode.Clamp"/> for <c>U</c>, <c>V</c>, and <c>W</c> Texture coordinates.</item>
    ///     <item>No Mip LOD bias (<c>0.0</c>).</item>
    ///     <item>A default maximum anisotropy of <c>16x</c>.</item>
    ///     <item>A comparison function that never passes (<see cref="CompareFunction.Never"/>).</item>
    ///     <item>A border color of black (<c>(0,0,0,0)</c>).</item>
    ///     <item>
    ///       No clamping on Mip-levels (<see cref="MinMipLevel"/> is <c>-<see cref="float.MaxValue"/></c> and
    ///       <see cref="MaxMipLevel"/> is <c><see cref="float.MaxValue"/></c>).
    ///     </item>
    ///   </list>
    /// </remarks>
    public static SamplerStateDescription Default => new();


    public static bool operator ==(SamplerStateDescription left, SamplerStateDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SamplerStateDescription left, SamplerStateDescription right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public readonly bool Equals(SamplerStateDescription other)
    {
        return Filter == other.Filter
            && AddressU == other.AddressU
            && AddressV == other.AddressV
            && AddressW == other.AddressW
            && MipMapLevelOfDetailBias.Equals(other.MipMapLevelOfDetailBias)
            && MaxAnisotropy == other.MaxAnisotropy
            && CompareFunction == other.CompareFunction
            && BorderColor.Equals(other.BorderColor)
            && MinMipLevel.Equals(other.MinMipLevel)
            && MaxMipLevel.Equals(other.MaxMipLevel);
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is SamplerStateDescription description && Equals(description);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Filter);
        hash.Add(AddressU);
        hash.Add(AddressV);
        hash.Add(AddressW);
        hash.Add(MipMapLevelOfDetailBias);
        hash.Add(MaxAnisotropy);
        hash.Add(CompareFunction);
        hash.Add(BorderColor);
        hash.Add(MinMipLevel);
        hash.Add(MaxMipLevel);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return $"Sampler State {{Filter: {Filter}, Address UVW: {AddressU}, {AddressV}, {AddressW}, Mip LOD Bias: {MipMapLevelOfDetailBias}, Max Anisotropy: {MaxAnisotropy}, Compare Function: {CompareFunction}, Border Color: {BorderColor}, Min/Max MipLevel: {MinMipLevel} / {MaxMipLevel}}}";
    }
}
