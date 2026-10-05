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
/// Concat multiple heightfields together multiplicatively 
/// </summary>
[DataContract]
[Display("Multiply")]
public class MultiplyOperator : IHeightfieldLayerBuilder
{
    /// <summary>
    /// Each heightfield listed contributes multiplicatively to the final height 
    /// </summary>
    public readonly List<IHeightfieldLayerBuilder> Layers = new();

    /// <inheritdoc/>
    public IEnumerable<ObjectUrl> GetInputFiles()
    {
        foreach (var heightfieldLayer in Layers)
        {
            foreach (var file in heightfieldLayer.GetInputFiles())
            {
                yield return file;
            }
        }
    }

    /// <inheritdoc/>
    public void BuildRuntimeRepresentation(float size, int subdivision, ICommandContext commandContext, IAssetFinder assetFinder, out IHeightfieldRuntimeLayer output, out float minHeight, out float maxHeight)
    {
        var runtime = new RuntimeLayer();
        minHeight = 0;
        maxHeight = 0;
        foreach (var layer in Layers)
        {
            layer.BuildRuntimeRepresentation(size, subdivision, commandContext, assetFinder, out output, out var thisMinHeight, out var thisMaxHeight);
            minHeight *= thisMinHeight;
            maxHeight *= thisMaxHeight;
            runtime.Layers.Add(output);
        }

        output = runtime;
    }

    /// <summary>
    /// Post-compilation representation of a <see cref="MultiplyOperator"/>  
    /// </summary>
    [DataContract]
    public record RuntimeLayer : IHeightfieldRuntimeLayer
    {
        /// <summary>
        /// Each heightfield listed contributes multiplicatively to the final height 
        /// </summary>
        public readonly List<IHeightfieldRuntimeLayer> Layers = new();

        /// <inheritdoc/>
        public IHeightfieldFunction BuildHeightfieldFunction()
        {
            var stack = new Collector();
            foreach (var layer in Layers)
            {
                var function = layer.BuildHeightfieldFunction();
                function.AppendTo(stack);
            }

            return stack.GetFunction();
        }

        /// <inheritdoc/>
        public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context)
        {
            IComputeScalar? prev = null;
            foreach (var layer in Layers)
            {
                var newSampler = layer.BuildGpuSideSampler(context);
                if (prev is null)
                    prev = newSampler;
                else
                    prev = new ComputeBinaryScalar(prev, newSampler, BinaryOperator.Multiply);
            }

            return prev ?? new ComputeFloat(0);
        }
    }

    private class Collector : IFunctionCollector
    {
        private IHandler handler = ConcreteHandler<Empty>.Instance;
        private IHeightfieldFunction current = new Empty();

        public IHeightfieldFunction GetFunction() => current;

        public void Append<T>(T newT) where T : IHeightfieldFunction
        {
            handler.Handle(newT, ref current, out handler);
        }

        private interface IHandler
        {
            public void Handle<T>(T newT, ref IHeightfieldFunction current, out IHandler next) where T : IHeightfieldFunction;
        }

        private class ConcreteHandler<TPrev> : IHandler where TPrev : IHeightfieldFunction
        {
            public static readonly ConcreteHandler<TPrev> Instance = new();

            public void Handle<T>(T newT, ref IHeightfieldFunction current, out IHandler next) where T : IHeightfieldFunction
            {
                if (current is Empty)
                {
                    current = newT;
                    next = ConcreteHandler<T>.Instance;
                }
                else
                {
                    current = new MultFunction<TPrev, T>((TPrev)current, newT);
                    next = ConcreteHandler<MultFunction<TPrev, T>>.Instance;
                }
            }
        }

        private struct Empty : IHeightfieldFunction
        {
            public void FillSamples(Span<Sample> samples) { }

            public void AppendTo(IFunctionCollector solution) { }
        }
    }

    private readonly record struct MultFunction<T1, T2>(T1 a, T2 b) : IHeightfieldFunction where T1 : IHeightfieldFunction where T2 : IHeightfieldFunction
    {
        public void FillSamples(Span<Sample> samples)
        {
            a.FillSamples(samples);

            Span<Sample> samplesA = stackalloc Sample[samples.Length];
            samples.CopyTo(samplesA);

            b.FillSamples(samples);

            for (var i = 0; i < samplesA.Length; i++)
                samples[i].Height *= samplesA[i].Height;
        }

        public void AppendTo(IFunctionCollector solution)
        {
            solution.Append(this);
        }
    }
}
