// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;
using AttachedReferenceManager = Stride.Core.Serialization.AttachedReferenceManager;

namespace Stride.Heightfield;

/// <summary>
/// A variant of <see cref="MaterialDisplacementMapFeature"/> with a bunch of features dedicated to
/// <see cref="IHeightfieldSource"/> and the <see cref="HeightfieldModelComponent"/>
/// </summary>
[DataContract("HeightfieldDisplacementFeature")]
[Display("Heightfield Displacement Feature")]
public class HeightfieldDisplacementFeature : MaterialFeature, IMaterialDisplacementFeature
{
    /// <summary>
    /// The source for the heightfield being rendered
    /// </summary>
    public required IHeightfieldRenderingSource HeightfieldSource { get; set; }

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

        // This is an on-build workaround. During build, HeightfieldSource is an empty proxy
        // created through ContentReferenceSerializer. Seems to be on purpose?
        // RetrieveGPUSideSampler in most case requires full data to provide the right sampler so we'll force load it.
        var source = HeightfieldSource;
        if (context.Content.TryGetAssetUrl(source, out _) == false)
        {
            var url = AttachedReferenceManager.GetUrl(source);
            source = (IHeightfieldRenderingSource)context.Content.Load(source.GetType(), url!);
        }

        context.Parameters.Set(HeightfieldDisplacementPropertiesKeys.HeightfieldSize, source.Size);
        context.Parameters.Set(HeightfieldDisplacementPropertiesKeys.HeightfieldMinHeight, source.MinHeight);
        context.Parameters.Set(HeightfieldDisplacementPropertiesKeys.HeightfieldMaxHeight, source.MaxHeight);
        context.Parameters.Set(HeightfieldDisplacementPropertiesKeys.HeightfieldSubdivision, source.Subdivision);

        // Workaround to inform compute colors that sampling is occurring from a vertex shader
        context.IsNotPixelStage = true;
        var inner = source.BuildGpuSideSampler(context);
        var displacementSource = inner.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialKeys.DisplacementMap, MaterialKeys.DisplacementValue));
        context.IsNotPixelStage = false;

        var beforePixelShader = new ShaderMixinSource();
        var displacementWindow = new ShaderClassSource("HeightfieldDisplacementWindow");
        if (Stage == DisplacementMapStage.Domain)
        {
            // We want to comply with tessellation, have the displacement window on the vertex stage,
            // while the offset is on the domain stage
            var vertexMixin = new ShaderMixinSource();
            vertexMixin.Mixins.Add(displacementWindow);
            vertexMixin.AddComposition("inner", displacementSource);
            context.SetStreamFinalModifier<MaterialDisplacementMapFeature>(MaterialShaderStage.Vertex, vertexMixin);
        }
        else
        {
            beforePixelShader.Mixins.Add(displacementWindow);
        }

        var normalMember = Stage == DisplacementMapStage.Vertex ? "meshNormal" : "normalWS";
        bool recomputeHeight = Stage == DisplacementMapStage.Domain; // Correct samples height post tessellation
        beforePixelShader.Mixins.Add(new ShaderClassSource("HeightfieldDisplacement", recomputeHeight, ComputeNormals, normalMember));
        beforePixelShader.AddComposition("inner", displacementSource);

        // Note: Using 'MaterialDisplacementMapFeature' to test the type specific conditional AddAdjacentEdgeAverageShaders,
        // will likely need a custom handler
        context.SetStreamFinalModifier<MaterialDisplacementMapFeature>(Stage == DisplacementMapStage.Vertex ? MaterialShaderStage.Vertex : MaterialShaderStage.Domain, beforePixelShader);
    }
}
