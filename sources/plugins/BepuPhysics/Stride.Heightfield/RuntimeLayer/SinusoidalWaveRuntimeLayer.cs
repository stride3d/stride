// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Heightfield.RuntimeLayer;

/// <summary>
/// A <see cref="IHeightfieldRuntimeLayer"/> representing a sinusoidal wave
/// </summary>
[DataContract]
public record SinusoidalWaveRuntimeLayer : SinusoidalWaveSharedData, IHeightfieldRuntimeLayer
{
    /// <summary>
    /// Matches the <see cref="IHeightfieldSource.Subdivision"/> this layer runs on
    /// </summary>
    public required int HeightfieldResolution { get; set; }

    /// <inheritdoc/>
    public IHeightfieldFunction BuildHeightfieldFunction()
    {
        var rezToBuff = (1f / HeightfieldResolution) * Tiling;
        return new Function(rezToBuff, HeightMultiplier);
    }

    /// <inheritdoc/>
    public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context)
    {
        return new ComputeBinaryScalar(new Shader{ Tiling = Tiling }, new ComputeFloat(HeightMultiplier), BinaryOperator.Multiply);
    }

    /// <summary>
    /// Shader used by <see cref="SinusoidalWaveRuntimeLayer.BuildGpuSideSampler"/> to offset the vertices of the heightfield mesh through the material system.
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

    private readonly record struct Function(Vector2 Tiling, float Multiplier) : IHeightfieldFunction
    {
        public void FillSamples(Span<Sample> samples)
        {
            foreach (ref var sample in samples)
            {
                var coord = (Vector2)sample.SampleCoord * Tiling;
                sample.Height = MathF.Sin(coord.X) * MathF.Cos(coord.Y) * Multiplier;
            }
        }

        public void AppendTo(IFunctionCollector solution) => solution.Append(this);
    }
}
