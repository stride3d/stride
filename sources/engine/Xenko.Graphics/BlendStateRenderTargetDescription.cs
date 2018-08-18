// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// Describes the blend state for a render target.
    /// </summary>
    [DataContract]
    public struct BlendStateRenderTargetDescription : IEquatable<BlendStateRenderTargetDescription>
    {
        /// <summary>
        /// Enable (or disable) blending. 
        /// </summary>
        public bool BlendEnable;

        /// <summary>
        /// This <see cref="Blend"/> specifies the first RGB data source and includes an optional pre-blend operation. 
        /// </summary>
        public Blend ColorSourceBlend;

        /// <summary>
        /// This <see cref="Blend"/> specifies the second RGB data source and includes an optional pre-blend operation. 
        /// </summary>
        public Blend ColorDestinationBlend;

        /// <summary>
        /// This <see cref="BlendFunction"/> defines how to combine the RGB data sources. 
        /// </summary>
        public BlendFunction ColorBlendFunction;

        /// <summary>
        /// This <see cref="Blend"/> specifies the first alpha data source and includes an optional pre-blend operation. Blend options that end in _COLOR are not allowed. 
        /// </summary>
        public Blend AlphaSourceBlend;

        /// <summary>
        /// This <see cref="Blend"/> specifies the second alpha data source and includes an optional pre-blend operation. Blend options that end in _COLOR are not allowed. 
        /// </summary>
        public Blend AlphaDestinationBlend;

        /// <summary>
        /// This <see cref="BlendFunction"/> defines how to combine the alpha data sources. 
        /// </summary>
        public BlendFunction AlphaBlendFunction;

        /// <summary>
        /// A write mask. 
        /// </summary>
        public ColorWriteChannels ColorWriteChannels;

        public bool Equals(BlendStateRenderTargetDescription other)
        {
            return BlendEnable == other.BlendEnable && ColorSourceBlend == other.ColorSourceBlend && ColorDestinationBlend == other.ColorDestinationBlend && ColorBlendFunction == other.ColorBlendFunction && AlphaSourceBlend == other.AlphaSourceBlend && AlphaDestinationBlend == other.AlphaDestinationBlend && AlphaBlendFunction == other.AlphaBlendFunction && ColorWriteChannels == other.ColorWriteChannels;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BlendStateRenderTargetDescription && Equals((BlendStateRenderTargetDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = BlendEnable.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ColorSourceBlend;
                hashCode = (hashCode * 397) ^ (int)ColorDestinationBlend;
                hashCode = (hashCode * 397) ^ (int)ColorBlendFunction;
                hashCode = (hashCode * 397) ^ (int)AlphaSourceBlend;
                hashCode = (hashCode * 397) ^ (int)AlphaDestinationBlend;
                hashCode = (hashCode * 397) ^ (int)AlphaBlendFunction;
                hashCode = (hashCode * 397) ^ (int)ColorWriteChannels;
                return hashCode;
            }
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
}
