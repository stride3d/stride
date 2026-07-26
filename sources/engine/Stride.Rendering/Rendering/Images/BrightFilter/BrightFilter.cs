// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering.Images;

/// <summary>
///   An image effect that extracts the brightest parts of an input image to produce a bright-pass result.
/// </summary>
/// <remarks>
///   This effect is typically used as a pre-pass for post-processing effects such as bloom or light streaks.
///   It computes the relative luminance of each pixel, builds a smooth selection mask from
///   <see cref="Threshold"/> and <see cref="Steepness"/>, and multiplies the original color by that mask.
/// </remarks>
[DataContract("BrightFilter")]
public class BrightFilter : ImageEffect
{
    // TODO: Add Brightpass filters based on average luminance and key value, taking into account the tonemap
    private readonly ImageEffectShader brightPassFilter;


    /// <summary>
    ///   Initializes a new instance of the <see cref="BrightFilter"/> class.
    /// </summary>
    public BrightFilter()
        : this("BrightFilterShader")
    {
        Threshold = 0.2f;
        Steepness = 1.0f;
        Color = new Color3(1.0f);
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="BrightFilter"/> class.
    /// </summary>
    /// <param name="brightPassShaderName">The name of the bright pass shader.</param>
    /// <exception cref="ArgumentNullException"><paramref name="brightPassShaderName"/> is <see langword="null"/>.</exception>
    public BrightFilter(string brightPassShaderName) : base(brightPassShaderName)
    {
        ArgumentNullException.ThrowIfNull(brightPassShaderName);

        brightPassFilter = new ImageEffectShader(brightPassShaderName);
    }


    /// <summary>
    ///   Gets or sets the brightness threshold used to decide when a pixel
    ///   starts contributing to the bright-pass result.
    /// </summary>
    /// <value>
    ///   The bright-pass threshold.
    ///   Lower values allow more pixels to pass through the filter.
    ///   Higher values restrict the result to only the brightest parts of the image.
    /// </value>
    /// <remarks>
    ///   This value is best understood relative to the HDR intensity range of the image and, conceptually,
    ///   to the scene white point used by the tone-mapping pipeline.
    ///   <para/>
    ///   The term <em>white point</em> refers to the scene intensity that is considered reference white
    ///   in the HDR pipeline, usually after exposure has been taken into account.
    ///   In this effect, <see cref="Threshold"/> should be understood relative to that expected intensity range.
    /// </remarks>
    /// <userdoc>
    ///   The intensity threshold used to identify bright areas.
    ///   Lower values keep more pixels; higher values isolate only the brightest ones.
    /// </userdoc>
    [DataMember(10)]
    [DefaultValue(0.2f)]
    public float Threshold { get; set; }

    /// <summary>
    ///   Gets or sets how selectively the filter responds around the threshold.
    /// </summary>
    /// <value>
    ///   This value affects the luminance remapping used before the smooth threshold is applied.
    ///   Higher values make the filter more selective and reduce the contribution of mid-bright pixels.
    ///   Lower values make the transition start earlier and allow more near-threshold pixels to contribute.
    /// </value>
    /// <userdoc>
    ///   The steepness of the threshold curve.
    ///   Higher values isolate only the brightest pixels;
    ///   lower values allow more near-threshold pixels to contribute.
    /// </userdoc>
    [DataMember(15)]
    [DefaultValue(1.0f)]
    public float Steepness { get; set; }

    /// <summary>
    ///   Gets or sets the color used to modulate the extracted bright areas.
    /// </summary>
    /// <value>The modulation color applied to the filtered result.</value>
    /// <remarks>
    ///   This is commonly used to tint the contribution that will later feed bloom, glare, or streak effects.
    /// </remarks>
    /// <userdoc>
    ///   Modulates the extracted bright areas with the provided color.
    ///   This affects the color of subsequent bloom or light-streak effects.
    /// </userdoc>
    [DataMember(20)]
    public Color3 Color { get; set; }


    protected override void InitializeCore()
    {
        base.InitializeCore();

        ToLoadAndUnload(brightPassFilter);
    }

    protected override void SetDefaultParameters()
    {
        Color = new Color3(1.0f);

        base.SetDefaultParameters();
    }

    protected override void DrawCore(RenderDrawContext context)
    {
        var input = GetInput(0);
        var output = GetOutput(0);

        if (input is null || output is null)
            return;
    
        brightPassFilter.Parameters.Set(BrightFilterShaderKeys.ThresholdOffset, Threshold);
        brightPassFilter.Parameters.Set(BrightFilterShaderKeys.BrightPassSteepness, Steepness);
        brightPassFilter.Parameters.Set(BrightFilterShaderKeys.ColorModulator, Color.ToColorSpace(GraphicsDevice.ColorSpace));
        
        brightPassFilter.SetInput(input);
        brightPassFilter.SetOutput(output);
        brightPassFilter.Draw(context);
    }
}
