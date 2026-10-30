// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Heightfield.RuntimeLayer;

/// <summary>
/// Compute the smooth minimum between two <see cref="IHeightfieldRuntimeLayer"/>
/// </summary>
[DataContract]
public record SmoothMinRuntimeLayer : IHeightfieldRuntimeLayer
{
    /// <summary>
    /// Each heightfield listed contributes additively to the final height 
    /// </summary>
    public required IHeightfieldRuntimeLayer? A, B;

    public float Smoothness = 4;

    /// <inheritdoc/>
    public IHeightfieldFunction BuildHeightfieldFunction()
    {
        var stack = new Collector(Smoothness);
        var functionA = A?.BuildHeightfieldFunction();
        var functionB = B?.BuildHeightfieldFunction();
        functionA?.AppendTo(stack);
        functionB?.AppendTo(stack);
        return stack.GetFunction();
    }

    /// <inheritdoc/>
    public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context)
    {
        var samplerA = A?.BuildGpuSideSampler(context) ?? new ComputeFloat(0f);
        var samplerB = B?.BuildGpuSideSampler(context) ?? new ComputeFloat(0f);
        return new ComputeSmoothMinScalar
        {
            A = samplerA,
            B = samplerB,
            Smoothness = Smoothness
        };
    }

    private class Collector(float smoothness) : IFunctionCollector
    {
        private IHandler handler = CollectA.Instance;
        private IHeightfieldFunction current = new Empty();

        public IHeightfieldFunction GetFunction() => current;

        public void Append<T>(T newT) where T : IHeightfieldFunction
        {
            handler.Handle(newT, smoothness, ref current, out handler);
        }

        private interface IHandler
        {
            public void Handle<T>(T newT, float smoothness, ref IHeightfieldFunction current, out IHandler next) where T : IHeightfieldFunction;
        }

        private class CollectA : IHandler
        {
            public static readonly CollectA Instance = new();

            public void Handle<T>(T newT, float smoothness, ref IHeightfieldFunction current, out IHandler next) where T : IHeightfieldFunction
            {
                next = new CollectB<T>(newT);
            }
        }

        private class CollectB<TA>(TA black) : IHandler where TA : IHeightfieldFunction
        {
            public void Handle<T>(T newT, float smoothness, ref IHeightfieldFunction current, out IHandler next) where T : IHeightfieldFunction
            {
                current = new SmoothMinFunction<TA, T>(black, newT, smoothness);
                next = null!;
            }
        }

        private struct Empty : IHeightfieldFunction
        {
            public void FillSamples(Span<Sample> samples) { }

            public void AppendTo(IFunctionCollector solution) { }
        }
    }

    private readonly record struct SmoothMinFunction<TA, TB>(TA FuncA, TB FuncB, float Smoothness) : IHeightfieldFunction
        where TA : IHeightfieldFunction
        where TB : IHeightfieldFunction
    {
        public unsafe void FillSamples(Span<Sample> samples)
        {
            Span<Sample> bSamples = stackalloc Sample[samples.Length];
            FuncA.FillSamples(samples);
            FuncB.FillSamples(bSamples);
            for (int i = 0; i < samples.Length; i++)
            {
                var a = samples[i].Height;
                var b = bSamples[i].Height;
                // quadratic smin, see https://iquilezles.org/articles/smin/
                float h = float.Max(Smoothness - float.Abs(a - b), 0f) / Smoothness;
                samples[i].Height = float.Min(a, b) - h * h * Smoothness * (1f / 4f);
            }
        }

        public void AppendTo(IFunctionCollector solution)
        {
            solution.Append(this);
        }
    }


    [DataContract(Inherited = true)]
    [Display("Smooth Min Operator")]
    [InlineProperty]
    public class ComputeSmoothMinScalar : ComputeNode, IComputeScalar
    {
        [MaybeNull] public required IComputeScalar A { get; set; }

        [MaybeNull] public required IComputeScalar B { get; set; }

        public required float Smoothness { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<IComputeNode> GetChildren([MaybeNull] object context = null)
        {
            if (A != null)
                yield return A;
            if (B != null)
                yield return B;
        }

        private const string CompositionNameA = "color1";
        private const string CompositionNameB = "color2";

        /// <inheritdoc/>
        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var blackShaderSource = A?.GenerateShaderSource(context, baseKeys);
            var whiteShaderSource = B?.GenerateShaderSource(context, baseKeys);

            var shaderSource = new ShaderClassSource("ComputeSmoothMinScalar", Smoothness);
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(shaderSource);
            if (blackShaderSource != null)
                mixin.AddComposition(CompositionNameA, blackShaderSource);
            if (whiteShaderSource != null)
                mixin.AddComposition(CompositionNameB, whiteShaderSource);

            return mixin;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Smooth Min operation";
        }
    }
}
