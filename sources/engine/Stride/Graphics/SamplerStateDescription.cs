// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a sampler state.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct SamplerStateDescription : IEquatable<SamplerStateDescription>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplerStateDescription"/> class.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="addressMode">The address mode.</param>
        public SamplerStateDescription(TextureFilter filter, TextureAddressMode addressMode) : this()
        {
            SetDefaults();
            Filter = filter;
            AddressU = AddressV = AddressW = addressMode;
        }

        /// <summary>
        /// Gets or sets filtering method to use when sampling a texture (see <see cref="TextureFilter"/>).
        /// </summary>
        public TextureFilter Filter;

        /// <summary>
        /// Gets or sets method to use for resolving a u texture coordinate that is outside the 0 to 1 range (see <see cref="TextureAddressMode"/>).
        /// </summary>
        public TextureAddressMode AddressU;

        /// <summary>
        /// Gets or sets method to use for resolving a v texture coordinate that is outside the 0 to 1 range.
        /// </summary>
        public TextureAddressMode AddressV;

        /// <summary>
        /// Gets or sets method to use for resolving a w texture coordinate that is outside the 0 to 1 range.
        /// </summary>
        public TextureAddressMode AddressW;

        /// <summary>
        /// Gets or sets offset from the calculated mipmap level.
        /// For example, if Direct3D calculates that a texture should be sampled at mipmap level 3 and MipLODBias is 2, then the texture will be sampled at mipmap level 5.
        /// </summary>
        public float MipMapLevelOfDetailBias;

        /// <summary>
        /// Gets or sets clamping value used if Anisotropy or ComparisonAnisotropy is specified in Filter. Valid values are between 1 and 16.
        /// </summary>
        public int MaxAnisotropy;

        /// <summary>
        /// Gets or sets a function that compares sampled data against existing sampled data. The function options are listed in <see cref="CompareFunction"/>.
        /// </summary>
        public CompareFunction CompareFunction;

        /// <summary>
        /// Gets or sets border color to use if <see cref="TextureAddressMode.Border"/> is specified for AddressU, AddressV, or AddressW. Range must be between 0.0 and 1.0 inclusive.
        /// </summary>
        public Color4 BorderColor;

        /// <summary>
        /// Gets or sets lower end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level and any level higher than that is less detailed.
        /// </summary>
        public float MinMipLevel;

        /// <summary>
        /// Gets or sets upper end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level and any level higher than that is less detailed. This value must be greater than or equal to MinLOD. To have no upper limit on LOD set this to a large value such as D3D11_FLOAT32_MAX.
        /// </summary>
        public float MaxMipLevel;

        /// <summary>
        /// Gets default values for this instance.
        /// </summary>
        public static SamplerStateDescription Default
        {
            get
            {
                var desc = new SamplerStateDescription();
                desc.SetDefaults();
                return desc;
            }
        }

        public bool Equals(SamplerStateDescription other)
        {
            return Filter == other.Filter && AddressU == other.AddressU && AddressV == other.AddressV && AddressW == other.AddressW && MipMapLevelOfDetailBias.Equals(other.MipMapLevelOfDetailBias) && MaxAnisotropy == other.MaxAnisotropy && CompareFunction == other.CompareFunction && BorderColor.Equals(other.BorderColor) && MinMipLevel.Equals(other.MinMipLevel) && MaxMipLevel.Equals(other.MaxMipLevel);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SamplerStateDescription && Equals((SamplerStateDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Filter;
                hashCode = (hashCode * 397) ^ (int)AddressU;
                hashCode = (hashCode * 397) ^ (int)AddressV;
                hashCode = (hashCode * 397) ^ (int)AddressW;
                hashCode = (hashCode * 397) ^ MipMapLevelOfDetailBias.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxAnisotropy;
                hashCode = (hashCode * 397) ^ (int)CompareFunction;
                hashCode = (hashCode * 397) ^ BorderColor.GetHashCode();
                hashCode = (hashCode * 397) ^ MinMipLevel.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxMipLevel.GetHashCode();
                return hashCode;
            }
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
}
