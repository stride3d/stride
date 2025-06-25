// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics;

[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct SamplerStateDescription : IEquatable<SamplerStateDescription>
{
    /// <summary>
    /// Describes a sampler state.
    /// </summary>
    public SamplerStateDescription(TextureFilter filter, TextureAddressMode addressMode) : this()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplerStateDescription"/> class.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="addressMode">The address mode.</param>
        SetDefaults();

        /// <summary>
        /// Gets or sets filtering method to use when sampling a texture (see <see cref="TextureFilter"/>).
        /// </summary>
        /// <summary>
        /// Gets or sets method to use for resolving a u texture coordinate that is outside the 0 to 1 range (see <see cref="TextureAddressMode"/>).
        /// </summary>
        /// <summary>
        /// Gets or sets method to use for resolving a v texture coordinate that is outside the 0 to 1 range.
        /// </summary>
        /// <summary>
        /// Gets or sets method to use for resolving a w texture coordinate that is outside the 0 to 1 range.
        /// </summary>
        /// <summary>
        /// Gets or sets offset from the calculated mipmap level.
        /// For example, if Direct3D calculates that a texture should be sampled at mipmap level 3 and MipLODBias is 2, then the texture will be sampled at mipmap level 5.
        /// </summary>
        /// <summary>
        /// Gets or sets clamping value used if Anisotropy or ComparisonAnisotropy is specified in Filter. Valid values are between 1 and 16.
        /// </summary>
        /// <summary>
        /// Gets or sets a function that compares sampled data against existing sampled data. The function options are listed in <see cref="CompareFunction"/>.
        /// </summary>
        /// <summary>
        /// Gets or sets border color to use if <see cref="TextureAddressMode.Border"/> is specified for AddressU, AddressV, or AddressW. Range must be between 0.0 and 1.0 inclusive.
        /// </summary>
        /// <summary>
        /// Gets or sets lower end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level and any level higher than that is less detailed.
        /// </summary>
        /// <summary>
        /// Gets or sets upper end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level and any level higher than that is less detailed. This value must be greater than or equal to MinLOD. To have no upper limit on LOD set this to a large value such as D3D11_FLOAT32_MAX.
        /// </summary>
        /// <summary>
        /// Gets default values for this instance.
        /// </summary>
        Filter = filter;
        AddressU = AddressV = AddressW = addressMode;
    }


    public TextureFilter Filter;

    public TextureAddressMode AddressU;

    public TextureAddressMode AddressV;

    public TextureAddressMode AddressW;

    public float MipMapLevelOfDetailBias;

    public int MaxAnisotropy;

    public CompareFunction CompareFunction;

    public Color4 BorderColor;

    public float MinMipLevel;

    public float MaxMipLevel;


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

    public override bool Equals(object obj)
    {
        return obj is SamplerStateDescription description && Equals(description);
    }

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
