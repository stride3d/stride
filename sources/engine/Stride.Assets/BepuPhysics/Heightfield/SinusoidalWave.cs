// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// Heightfield built from a simple repeating discretized sin/cos waveform
/// </summary>
[DataContract]
public record SinusoidalWave : SinusoidalWaveSharedData, IHeightfieldLayerBuilder
{
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
            Tiling = Tiling,
            HeightMultiplier = HeightMultiplier,
            FieldResolution = subdivision
        };
        minHeight = -HeightMultiplier;
        maxHeight = HeightMultiplier;
    }

    /// <summary>
    /// Post-compilation representation of a <see cref="SinusoidalWave"/>, effectively a no-op compared to its pre-compilation counterpart
    /// </summary>
    [DataContract]
    public record RuntimeLayer : SinusoidalWaveSharedData, IHeightfieldRuntimeLayer
    {
        /// <summary>
        /// Matches the <see cref="IHeightfieldSource.Subdivision"/> this layer runs on
        /// </summary>
        public required int FieldResolution { get; set; }

        /// <inheritdoc/>
        public IHeightfieldFunction BuildHeightfieldFunction()
        {
            var rezToBuff = (1f / FieldResolution) * Tiling;
            return new Function(rezToBuff, HeightMultiplier);
        }

        /// <inheritdoc/>
        public IComputeScalar BuildGPUSideSampler(MaterialGeneratorContext context)
        {
            return new ComputeBinaryScalar(new Shader{ Tiling = Tiling }, new ComputeFloat(HeightMultiplier), BinaryOperator.Multiply);
        }

        /// <summary>
        /// Shader used by <see cref="RuntimeLayer.BuildGPUSideSampler"/> to offset the vertices of the heightfield mesh through the material system.
        /// Matches the physics-side representation of the wave.
        /// </summary>
        [DataContract]
        public class Shader : IComputeScalar
        {
            private const string ShaderName = "HeightfieldSinusoidalWaveShader";

            /// <inheritdoc cref="SinusoidalWaveSharedData.Tiling" />
            public required Vector2 Tiling { get; set; }

            /// <inheritdoc/>
            public IEnumerable<IComputeNode> GetChildren(object? context = null)
            {
                yield break;
            }

            /// <inheritdoc/>
            public ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
            {
                var shaderSource = new ShaderClassSource(ShaderName, Tiling.X, Tiling.Y);
                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(shaderSource);
                return mixin;
            }

            /// <inheritdoc/>
            public override string ToString() => ShaderName;
        }
    }

    private readonly record struct Function(Vector2 Tiling, float Multiplier) : IHeightfieldFunction
    {
        public void FillSamples(Span<Sample> samples)
        {
            foreach (ref var sample in samples)
            {
                var coord = (Vector2)sample.SampleCoord * Tiling;
                sample.Height += MathF.Sin(coord.X) * MathF.Cos(coord.Y) * Multiplier;
            }
        }

        public void AppendTo(IFunctionCollector solution) => solution.Append(this);
    }
}
