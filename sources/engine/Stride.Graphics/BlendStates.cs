// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Defines a set of built-in <see cref="BlendStateDescription"/>s for common blending configurations.
/// </summary>
public static class BlendStates
{
    static BlendStates()
    {
        var colorDisabledDescription = BlendStateDescription.Default;
        colorDisabledDescription.RenderTargets[0].ColorWriteChannels = ColorWriteChannels.None;
        ColorDisabled = colorDisabledDescription;
    }


    /// <summary>
    ///   A built-in Blend State description with default settings, that is <strong>no blend at all</strong>.
    /// </summary>
    /// <inheritdoc cref="BlendStateDescription.Default" path="/remarks" />
    public static readonly BlendStateDescription Default = BlendStateDescription.Default;

    /// <summary>
    ///   A built-in Blend State description with settings for <strong>additive blending</strong>,
    ///   that is adding the destination data to the source data without using alpha.
    /// </summary>
    /// <remarks>
    ///   This built-in state object has the following settings for the first Render Target:
    ///   <list type="table">
    ///     <listheader>
    ///       <term>Property</term> <description>Value</description>
    ///     </listheader>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.ColorSourceBlend"/></term>
    ///       <description><see cref="Blend.SourceAlpha"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.AlphaSourceBlend"/></term>
    ///       <description><see cref="Blend.SourceAlpha"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.ColorDestinationBlend"/></term>
    ///       <description><see cref="Blend.One"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.AlphaDestinationBlend"/></term>
    ///       <description><see cref="Blend.One"/></description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public static readonly BlendStateDescription Additive = new(sourceBlend: Blend.SourceAlpha, destinationBlend: Blend.One);

    /// <summary>
    ///   A built-in Blend State description with settings for <strong>alpha blending</strong>,
    ///   that is blending the source and destination data using alpha.
    /// </summary>
    /// <remarks>
    ///   This built-in state object has the following settings for the first Render Target:
    ///   <list type="table">
    ///     <listheader>
    ///       <term>Property</term> <description>Value</description>
    ///     </listheader>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.ColorSourceBlend"/></term>
    ///       <description><see cref="Blend.One"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.AlphaSourceBlend"/></term>
    ///       <description><see cref="Blend.One"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.ColorDestinationBlend"/></term>
    ///       <description><see cref="Blend.InverseSourceAlpha"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.AlphaDestinationBlend"/></term>
    ///       <description><see cref="Blend.InverseSourceAlpha"/></description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public static readonly BlendStateDescription AlphaBlend = new(sourceBlend: Blend.One, destinationBlend: Blend.InverseSourceAlpha);

    /// <summary>
    ///   A built-in Blend State description with settings for blending with <strong>non-premultipled alpha</strong>,
    ///   that is blending source and destination data using alpha while assuming the color data contains no alpha information.
    /// </summary>
    /// <remarks>
    ///   This built-in state object has the following settings for the first Render Target:
    ///   <list type="table">
    ///     <listheader>
    ///       <term>Property</term> <description>Value</description>
    ///     </listheader>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.ColorSourceBlend"/></term>
    ///       <description><see cref="Blend.SourceAlpha"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.AlphaSourceBlend"/></term>
    ///       <description><see cref="Blend.SourceAlpha"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.ColorDestinationBlend"/></term>
    ///       <description><see cref="Blend.InverseSourceAlpha"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.AlphaDestinationBlend"/></term>
    ///       <description><see cref="Blend.InverseSourceAlpha"/></description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public static readonly BlendStateDescription NonPremultiplied = new(sourceBlend: Blend.SourceAlpha, destinationBlend: Blend.InverseSourceAlpha);

    /// <summary>
    ///   A built-in Blend State description with settings for <strong>opaque blending</strong>,
    ///   that is overwriting the destination with the source data.
    /// </summary>
    /// <remarks>
    ///   This built-in state object has the following settings for the first Render Target:
    ///   <list type="table">
    ///     <listheader>
    ///       <term>Property</term> <description>Value</description>
    ///     </listheader>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.ColorSourceBlend"/></term>
    ///       <description><see cref="Blend.One"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.AlphaSourceBlend"/></term>
    ///       <description><see cref="Blend.One"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.ColorDestinationBlend"/></term>
    ///       <description><see cref="Blend.Zero"/></description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="BlendStateRenderTargetDescription.AlphaDestinationBlend"/></term>
    ///       <description><see cref="Blend.Zero"/></description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public static readonly BlendStateDescription Opaque = new(sourceBlend: Blend.One, destinationBlend: Blend.Zero);

    /// <summary>
    ///   A built-in Blend State description with settings for <strong>disabling color rendering</strong> on the first Render Target (target 0),
    ///   that is rendering only to the Depth-Stencil Buffer.
    /// </summary>
    /// <remarks>
    ///   This is the same as the <see cref="Default"/> state, but with the first Render Target's color write channels disabled.
    /// </remarks>
    public static readonly BlendStateDescription ColorDisabled;
}
