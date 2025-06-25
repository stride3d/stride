// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
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
    /// <summary>
    ///   Initializes a new instance of the <see cref="SamplerStateDescription"/> structure.
    /// </summary>
    /// <param name="filter">The Texture filtering mode.</param>
    /// <param name="addressMode">The Texture addressing mode for U, V, and W coordinates.</param>
    public SamplerStateDescription(TextureFilter filter, TextureAddressMode addressMode) : this()
    {
        SetDefaults();

        Filter = filter;
        AddressU = AddressV = AddressW = addressMode;
    }


    /// <summary>
    ///   The filtering method to use when sampling a Texture.
    /// </summary>
    public TextureFilter Filter;

    /// <summary>
    ///   The method to use for resolving a U texture coordinate that is outside the [0, 1] range.
    /// </summary>
    public TextureAddressMode AddressU;

    /// <summary>
    ///   The method to use for resolving a V texture coordinate that is outside the [0, 1] range.
    /// </summary>
    public TextureAddressMode AddressV;

    /// <summary>
    ///   The method to use for resolving a W texture coordinate that is outside the [0, 1] range.
    /// </summary>
    public TextureAddressMode AddressW;

    /// <summary>
    ///   The offset to apply from the calculated mipmap level.
    /// </summary>
    /// <remarks>
    ///   For example, if a Texture should be sampled at mipmap level 3 and <see cref="MipMapLevelOfDetailBias"/>
    ///   is 2, then the Texture will be sampled at mipmap level 5.
    /// </remarks>
    public float MipMapLevelOfDetailBias;

    /// <summary>
    ///   The clamping value used if <see cref="TextureFilter.Anisotropic"/> or <see cref="TextureFilter.ComparisonAnisotropic"/>
    ///   is specified in <see cref="Filter"/>. Valid values are between 1 and 16.
    /// </summary>
    public int MaxAnisotropy;

    /// <summary>
    ///   A function that compares sampled data against existing sampled data.
    /// </summary>
    /// <remarks>
    ///   This function will be used when specifying one of the comparison filtering modes in
    ///   <see cref="Filter"/>.
    /// </remarks>
    public CompareFunction CompareFunction;

    /// <summary>
    ///   The border color to use if <see cref="TextureAddressMode.Border"/> is specified for
    ///   <see cref="AddressU"/>, <see cref="AddressV"/>, or <see cref="AddressW"/>.
    /// </summary>
    public Color4 BorderColor;

    /// <summary>
    ///   The lower end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap
    ///   level and any level higher than that is less detailed.
    /// </summary>
    public float MinMipLevel;

    /// <summary>
    ///   The upper end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap
    ///   level and any level higher than that is less detailed.
    /// </summary>
    /// <remarks>
    ///   This value must be greater than or equal to <see cref="MinMipLevel"/>.
    ///   To have no upper limit set this to a large value such as <see cref="float.MaxValue"/>.
    /// </remarks>
    public float MaxMipLevel;


    /// <summary>
    ///   Returns a <see cref="SamplerStateDescription"/> with default values.
    /// </summary>
    public static SamplerStateDescription Default
    {
        get
        {
            Unsafe.SkipInit(out SamplerStateDescription desc);
            desc.SetDefaults();
            return desc;
        }
    }


    public static bool operator ==(SamplerStateDescription left, SamplerStateDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SamplerStateDescription left, SamplerStateDescription right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public bool Equals(SamplerStateDescription other)
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
    public override bool Equals(object obj)
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

    /// <summary>
    ///   Sets the default values for this instance.
    /// </summary>
    private void SetDefaults()
    {
        Filter = TextureFilter.Linear;
        AddressU = TextureAddressMode.Clamp;
        AddressV = TextureAddressMode.Clamp;
        AddressW = TextureAddressMode.Clamp;
        BorderColor = new Color4();
        MaxAnisotropy = 16;
        MinMipLevel = -float.MaxValue;
        MaxMipLevel = float.MaxValue;
        MipMapLevelOfDetailBias = 0.0f;
        CompareFunction = CompareFunction.Never;
    }
}
