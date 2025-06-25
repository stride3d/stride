// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct BlendStateDescription : IEquatable<BlendStateDescription>
{
    /// <summary>
    /// Describes a blend state.
    /// </summary>
    public BlendStateDescription(Blend sourceBlend, Blend destinationBlend) : this()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlendStateDescription"/> class.
        /// </summary>
        /// <param name="sourceBlend">The source blend.</param>
        /// <param name="destinationBlend">The destination blend.</param>
        SetDefaults();
        RenderTargets[0].BlendEnable = true;
        RenderTargets[0].ColorSourceBlend = sourceBlend;
        RenderTargets[0].ColorDestinationBlend = destinationBlend;
        RenderTargets[0].AlphaSourceBlend = sourceBlend;
        RenderTargets[0].AlphaDestinationBlend = destinationBlend;
    }

        /// <summary>
        /// Setup this blend description with defaults value.
        /// </summary>
    public void SetDefaults()
    {
        AlphaToCoverageEnable = false;
        IndependentBlendEnable = false;

        /// <summary>
        /// Gets default values for this instance.
        /// </summary>
        for (int i = 0; i < RenderTargets.Count; i++)
        {
            ref var renderTarget = ref RenderTargets[i];

            renderTarget.BlendEnable = false;
            renderTarget.ColorSourceBlend = Blend.One;
            renderTarget.ColorDestinationBlend = Blend.Zero;
            renderTarget.ColorBlendFunction = BlendFunction.Add;

            renderTarget.AlphaSourceBlend = Blend.One;
            renderTarget.AlphaDestinationBlend = Blend.Zero;
            renderTarget.AlphaBlendFunction = BlendFunction.Add;

            renderTarget.ColorWriteChannels = ColorWriteChannels.All;
        }
    }

        /// <summary>
        /// Determines whether or not to use alpha-to-coverage as a multisampling technique when setting a pixel to a rendertarget. 
        /// </summary>

    public bool AlphaToCoverageEnable;

    public bool IndependentBlendEnable;

    public RenderTargetBlendStates RenderTargets;

    #region Render Targets inline array

    [System.Runtime.CompilerServices.InlineArray(SIMULTANEOUS_RENDERTARGET_COUNT)]
    public struct RenderTargetBlendStates
    {
        private const int SIMULTANEOUS_RENDERTARGET_COUNT = 8;

        /// <summary>
        /// Set to true to enable independent blending in simultaneous render targets.  If set to false, only the RenderTarget[0] members are used. RenderTarget[1..7] are ignored. 
        /// </summary>
        public readonly int Count => SIMULTANEOUS_RENDERTARGET_COUNT;

        private BlendStateRenderTargetDescription _renderTarget0;


        /// <summary>
        /// An array of render-target-blend descriptions (see <see cref="BlendStateRenderTargetDescription"/>); these correspond to the eight rendertargets  that can be set to the output-merger stage at one time. 
        /// </summary>
        /// <inheritdoc/>
        [UnscopedRef]
        public Span<BlendStateRenderTargetDescription> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref _renderTarget0, SIMULTANEOUS_RENDERTARGET_COUNT);
        }

        /// <inheritdoc/>
        [UnscopedRef]
        public readonly ReadOnlySpan<BlendStateRenderTargetDescription> AsReadOnlySpan()
        {
            return MemoryMarshal.CreateReadOnlySpan(ref System.Runtime.CompilerServices.Unsafe.AsRef(in _renderTarget0), SIMULTANEOUS_RENDERTARGET_COUNT);
        }
    }

    #endregion


        /// <inheritdoc/>
    public readonly bool Equals(BlendStateDescription other)
    {
        if (AlphaToCoverageEnable != other.AlphaToCoverageEnable ||
            IndependentBlendEnable != other.IndependentBlendEnable)
            return false;

        return RenderTargets.AsReadOnlySpan().SequenceEqual(other.RenderTargets);
    }

    public override readonly bool Equals(object obj)
    {
        return obj is BlendStateDescription description && Equals(description);
    }

    public static bool operator ==(BlendStateDescription left, BlendStateDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BlendStateDescription left, BlendStateDescription right)
    {
        return !left.Equals(right);
    }

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AlphaToCoverageEnable);
        hash.Add(IndependentBlendEnable);
        hash.Add(RenderTargets);
        return hash.ToHashCode();
    }
}
