// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Graphics;

/// <summary>
///   A description of a <strong>Blend State</strong>, which defines how colors are blended when rendering to one
///   or multiple Render Targets.
/// </summary>
/// <remarks>
///   This structure controls transparency, color mixing, and blend modes across all the Render Targets. Modify this to achieve effects
///   like alpha blending, additive blending, or custom shader-based blends.
///   It also controls whether to use <em>alpha-to-coverage</em> as a multi-sampling technique when writing a pixel to a Render Target.
/// </remarks>
/// <seealso cref="BlendStates"/>
[DataContract]
[DataSerializer(typeof(BlendStateDescription.Serializer))]
[StructLayout(LayoutKind.Sequential)]
public struct BlendStateDescription : IEquatable<BlendStateDescription>
{
    #region Default values

    /// <summary>
    ///   Default value for <see cref="AlphaToCoverageEnable"/>.
    /// </summary>
    public const bool DefaultAlphaToCoverageEnable = false;
    /// <summary>
    ///   Default value for <see cref="IndependentBlendEnable"/>.
    /// </summary>
    public const bool DefaultIndependentBlendEnable = false;

    #endregion

    /// <summary>
    ///   Initializes a new instance of the <see cref="BlendStateDescription"/> structure
    ///   with default values.
    /// </summary>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public BlendStateDescription()
    {
        SetDefaultRenderTargetDescriptions();
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="BlendStateDescription"/> structure
    ///   with default values, and the specified blending for the first Render Target.
    /// </summary>
    /// <param name="sourceBlend">The source blend.</param>
    /// <param name="destinationBlend">The destination blend.</param>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public BlendStateDescription(Blend sourceBlend, Blend destinationBlend) : this()
    {
        SetDefaultRenderTargetDescriptions();
        RenderTargets[0].BlendEnable = true;
        RenderTargets[0].ColorSourceBlend = sourceBlend;
        RenderTargets[0].ColorDestinationBlend = destinationBlend;
        RenderTargets[0].AlphaSourceBlend = sourceBlend;
        RenderTargets[0].AlphaDestinationBlend = destinationBlend;
    }

    /// <summary>
    ///   A Blend State description with default values.
    /// </summary>
    /// <remarks>
    ///   The default values are:
    ///   <list type="bullet">
    ///     <item>
    ///       <term>Alpha-to-Coverage</term>
    ///       <description>Disabled</description>
    ///     </item>
    ///     <item>
    ///       <term>Independent Blending</term>
    ///       <description>Disabled. Only enable blend for the first Render Target</description>
    ///     </item>
    ///     <item>Disable blending for all the Render Targets.</item>
    ///   </list>
    /// </remarks>
    public static readonly BlendStateDescription Default = new();

    /// <summary>
    ///   Sets default values for this Blend State Description.
    /// </summary>
    private void SetDefaultRenderTargetDescriptions()
    {
        var defaultRenderTargetDesc = BlendStateRenderTargetDescription.Default;

        for (int i = 0; i < RenderTargets.Count; i++)
        {
            RenderTargets[i] = defaultRenderTargetDesc;
        }
    }


    /// <summary>
    ///   A value that determines whether or not to use <strong>alpha-to-coverage</strong> as a multi-sampling technique
    ///   when writing a pixel to a Render Target.
    /// </summary>
    /// <remarks>
    ///   Alpha-to-coverage is a technique that uses the alpha value of a pixel to determine how much coverage it should have
    ///   in a multi-sampled anti-aliasing (MSAA) scenario.
    ///   This can help achieve smoother edges in transparent textures by blending the coverage of the pixel based on its alpha value.
    /// </remarks>
    public bool AlphaToCoverageEnable = DefaultAlphaToCoverageEnable;

    /// <summary>
    ///   A value indicating whether to enable <strong>independent blending</strong> in simultaneous Render Targets,
    ///   meaning per-Render Target blending settings.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If set to <see langword="true"/>, each of the <see cref="RenderTargets"/> can have its own blend settings.
    ///   </para>
    ///   <para>
    ///     If set to <see langword="false"/>, only the first Render Target (<c>RenderTargets[0]</c>) is taken into account.
    ///     The others (<c>RenderTargets[1]</c> to <c>RenderTargets[7]</c>) are ignored.
    ///   </para>
    /// </remarks>
    public bool IndependentBlendEnable = DefaultIndependentBlendEnable;

    /// <summary>
    ///   An array of Render Target blend descriptions (see <see cref="BlendStateRenderTargetDescription"/>);
    ///   these correspond to the eight Render Targets that can be set to the output-merger stage at one time.
    /// </summary>
    public RenderTargetBlendStates RenderTargets;

    #region Render Targets inline array

    /// <summary>
    ///   A structure that contains an inline array of <see cref="BlendStateRenderTargetDescription"/> for up to eight render targets.
    /// </summary>
    [System.Runtime.CompilerServices.InlineArray(SIMULTANEOUS_RENDERTARGET_COUNT)]
    public struct RenderTargetBlendStates
    {
        private const int SIMULTANEOUS_RENDERTARGET_COUNT = 8;

        /// <summary>
        ///   Gets the number of Render Target blend descriptions in a Blend State Description.
        /// </summary>
        public readonly int Count => SIMULTANEOUS_RENDERTARGET_COUNT;

        private BlendStateRenderTargetDescription _renderTarget0;


        /// <summary>
        ///   Returns a writable span of <see cref="BlendStateRenderTargetDescription"/> for the Render Targets.
        /// </summary>
        /// <returns>A <see cref="Span{T}"/> of Blend State descriptions for the Render Targets.</returns>
        [UnscopedRef]
        public Span<BlendStateRenderTargetDescription> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref _renderTarget0, SIMULTANEOUS_RENDERTARGET_COUNT);
        }

        /// <summary>
        ///   Returns a read-only span of <see cref="BlendStateRenderTargetDescription"/> for the Render Targets.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of Blend State descriptions for the Render Targets.</returns>
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

        return RenderTargets.AsReadOnlySpan().SequenceEqual(other.RenderTargets.AsReadOnlySpan());
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AlphaToCoverageEnable);
        hash.Add(IndependentBlendEnable);

        scoped ReadOnlySpan<BlendStateRenderTargetDescription> renderTargetsSpan = RenderTargets.AsReadOnlySpan();
        for (int i = 0; i < renderTargetsSpan.Length; i++)
            hash.Add(renderTargetsSpan[i]);

        return hash.ToHashCode();
    }

    // Custom serializer needed because RenderTargetBlendStates uses [InlineArray] with a private
    // backing field, which Stride's auto-generated serializer cannot access.
    internal class Serializer : DataSerializer<BlendStateDescription>
    {
        private DataSerializer<BlendStateRenderTargetDescription> _renderTargetSerializer;

        public override void Initialize(SerializerSelector serializerSelector)
        {
            _renderTargetSerializer = serializerSelector.GetSerializer<BlendStateRenderTargetDescription>();
        }

        public override void Serialize(ref BlendStateDescription obj, ArchiveMode mode, SerializationStream stream)
        {
            stream.Serialize(ref obj.AlphaToCoverageEnable);
            stream.Serialize(ref obj.IndependentBlendEnable);
            for (int i = 0; i < obj.RenderTargets.Count; i++)
                _renderTargetSerializer.Serialize(ref obj.RenderTargets[i], mode, stream);
        }
    }
}
