// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;
using AttachedReferenceManager = Stride.Core.Serialization.AttachedReferenceManager;

namespace Stride.BepuPhysics.Definitions.Heightfield;

/// <summary>
/// A variant of <see cref="MaterialDisplacementMapFeature"/> with a bunch of features dedicated to <see cref="IHeightfieldSource"/> and the <see cref="HeightfieldModelComponent"/>
/// </summary>
[DataContract("HeightfieldDisplacementFeature")]
[Display("Heightfield Displacement Feature")]
public class HeightfieldDisplacementFeature : MaterialFeature, IMaterialDisplacementFeature
{
    /// <summary>
    /// The source for the heightfield being rendered
    /// </summary>
    public required IHeightfieldSource HeightfieldSource { get; set; }

    /// <summary>
    /// Whether to compute the normals per vertex, has a significant performance overhead
    /// </summary>
    [DefaultValue(false)]
    public bool ComputeNormals { get; set; } = false;

    /// <summary>
    /// In which stage will the displacement occur.
    /// </summary>
    [DefaultValue(DisplacementMapStage.Vertex)]
    [Display("Shader Stage")]
    public DisplacementMapStage Stage { get; set; } = DisplacementMapStage.Vertex;

    /// <inheritdoc/>
    public override void GenerateShader(MaterialGeneratorContext context)
    {
        if (HeightfieldSource == null!)
            return;

        // This is an on-build workaround. During build, HeightfieldSource is not fully loaded, just an empty proxy.
        // RetrieveGPUSideSampler in most case requires full data before it can provide the right sampler.
        // No clue how I can ensure HeightfieldSource is properly and entirely deserialized when the MaterialCompiler calls into this function
        var source = HeightfieldSource;
        if (context.Content.TryGetAssetUrl(source, out _) == false)
        {
            var url = AttachedReferenceManager.GetUrl(source);
            source = (IHeightfieldSource)context.Content.Load(source.GetType(), url);
        }

        var materialStage = (MaterialShaderStage)Stage;

        var positionMember = materialStage == MaterialShaderStage.Vertex ? "Position" : "PositionWS";
        var normalMember = materialStage == MaterialShaderStage.Vertex ? "meshNormal" : "normalWS";
        var mixin = new ShaderMixinSource();
        mixin.Mixins.Add(new ShaderClassSource("HeightfieldDisplacement", positionMember, ComputeNormals, normalMember));

        // Workaround to inform compute colors that sampling is occurring from a vertex shader
        context.IsNotPixelStage = materialStage != MaterialShaderStage.Pixel;
        var inner = source.BuildGPUSideSampler(context);
        var innerShaderSource = inner.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialKeys.DisplacementMap, MaterialKeys.DisplacementValue));
        context.IsNotPixelStage = false;

        mixin.AddComposition("inner", innerShaderSource);
        // N.B: Using 'MaterialDisplacementMapFeature' to test the type specific conditional AddAdjacentEdgeAverageShaders,
        // will likely need a custom handler
        context.SetStreamFinalModifier<MaterialDisplacementMapFeature>(materialStage, mixin);
    }
}
