// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
public struct BlendStateRenderTargetDescription : IEquatable<BlendStateRenderTargetDescription>
{
    /// <summary>
    /// Describes the blend state for a render target.
    /// </summary>
    public bool BlendEnable;

    public Blend ColorSourceBlend;

    public Blend ColorDestinationBlend;

    public BlendFunction ColorBlendFunction;

    public Blend AlphaSourceBlend;

    public Blend AlphaDestinationBlend;

    public BlendFunction AlphaBlendFunction;

    public ColorWriteChannels ColorWriteChannels;


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

    public override readonly bool Equals(object obj)
    {
        return obj is BlendStateRenderTargetDescription description && Equals(description);
    }

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
        /// <summary>
        /// Enable (or disable) blending. 
        /// </summary>
        /// <summary>
        /// This <see cref="Blend"/> specifies the first RGB data source and includes an optional pre-blend operation. 
        /// </summary>
        /// <summary>
        /// This <see cref="Blend"/> specifies the second RGB data source and includes an optional pre-blend operation. 
        /// </summary>
        /// <summary>
        /// This <see cref="BlendFunction"/> defines how to combine the RGB data sources. 
        /// </summary>
        /// <summary>
        /// This <see cref="Blend"/> specifies the first alpha data source and includes an optional pre-blend operation. Blend options that end in _COLOR are not allowed. 
        /// </summary>
        /// <summary>
        /// This <see cref="Blend"/> specifies the second alpha data source and includes an optional pre-blend operation. Blend options that end in _COLOR are not allowed. 
        /// </summary>
        /// <summary>
        /// This <see cref="BlendFunction"/> defines how to combine the alpha data sources. 
        /// </summary>
        /// <summary>
        /// A write mask. 
        /// </summary>
        return !left.Equals(right);
    }
}
