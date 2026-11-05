// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Heightfield.RuntimeLayer;

/// <summary>
/// <see cref="IHeightfieldRuntimeLayer"/> of a constant fixed height
/// </summary>
[DataContract]
public record FixedHeightRuntimeLayer : IHeightfieldRuntimeLayer
{
    public float Height { get; set; } = 10;

    /// <inheritdoc/>
    public IHeightfieldFunction BuildHeightfieldFunction() => new Function(Height);

    /// <inheritdoc/>
    public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context) => new ComputeFloat(Height);

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
