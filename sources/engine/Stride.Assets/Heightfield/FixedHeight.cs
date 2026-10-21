// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Heightfield.Assets;

/// <summary>
/// Heightfield as a plane set to a fixed height
/// </summary>
[DataContract]
public record FixedHeight : IHeightfieldLayerBuilder
{
    public float Height { get; set; } = 10;

    /// <inheritdoc/>
    public IEnumerable<ObjectUrl> GetInputFiles()
    {
        yield break;
    }

    /// <inheritdoc/>
    public void BuildRuntimeRepresentation(float size, int subdivision, ICommandContext commandContext, IAssetFinder assetFinder, out IHeightfieldRuntimeLayer runtimeLayer, out float minHeight, out float maxHeight)
    {
        runtimeLayer = new RuntimeLayer
        {
            Height = Height,
        };
        minHeight = Height;
        maxHeight = Height;
    }

    /// <summary>
    /// Post-compilation representation of a <see cref="FixedHeight"/>, effectively a no-op compared to its pre-compilation counterpart
    /// </summary>
    [DataContract]
    public record RuntimeLayer : IHeightfieldRuntimeLayer
    {
        public float Height { get; set; } = 10;

        /// <inheritdoc/>
        public IHeightfieldFunction BuildHeightfieldFunction() => new Function(Height);

        /// <inheritdoc/>
        public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context) => new ComputeFloat(Height);
    }

    private readonly record struct Function(float Height) : IHeightfieldFunction
    {
        public void FillSamples(Span<Sample> samples)
        {
            foreach (ref var sample in samples)
                sample.Height = Height;
        }

        public void AppendTo(IFunctionCollector solution) => solution.Append(this);
    }
}
