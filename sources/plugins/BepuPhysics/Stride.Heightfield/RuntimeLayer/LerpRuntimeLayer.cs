// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Heightfield.RuntimeLayer;

/// <summary>
/// Interpolates between two <see cref="IHeightfieldRuntimeLayer"/>
/// </summary>
[DataContract]
public record LerpRuntimeLayer : IHeightfieldRuntimeLayer
{
    /// <summary>
    /// Each heightfield listed contributes additively to the final height 
    /// </summary>
    public required IHeightfieldRuntimeLayer? Black, White, BlendFactor;

    /// <inheritdoc/>
    public IHeightfieldFunction BuildHeightfieldFunction()
    {
        var stack = new Collector();
        var blackFunction = Black?.BuildHeightfieldFunction();
        var whiteFunction = White?.BuildHeightfieldFunction();
        var blendFunction = BlendFactor?.BuildHeightfieldFunction();
        blackFunction?.AppendTo(stack);
        whiteFunction?.AppendTo(stack);
        blendFunction?.AppendTo(stack);
        return stack.GetFunction();
    }

    /// <inheritdoc/>
    public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context)
    {
        var blackSampler = Black?.BuildGpuSideSampler(context) ?? new ComputeFloat(0f);
        var whiteSampler = White?.BuildGpuSideSampler(context) ?? new ComputeFloat(0f);
        var blendSampler = BlendFactor?.BuildGpuSideSampler(context) ?? new ComputeFloat(0f);
        return new ComputeClampedLerpScalar
        {
            Black = blackSampler,
            White = whiteSampler,
            Blend = blendSampler
        };
    }

    private class Collector : IFunctionCollector
    {
        private IHandler handler = CollectBlack.Instance;
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

        private class CollectBlack : IHandler
        {
            public static readonly CollectBlack Instance = new();

            public void Handle<T>(T newT, ref IHeightfieldFunction current, out IHandler next) where T : IHeightfieldFunction
            {
                next = new CollectWhite<T>(newT);
            }
        }

        private class CollectWhite<TBlack>(TBlack black) : IHandler where TBlack : IHeightfieldFunction
        {
            public void Handle<T>(T newT, ref IHeightfieldFunction current, out IHandler next) where T : IHeightfieldFunction
            {
                next = new CollectBlend<TBlack, T>(black, newT);
            }
        }

        private class CollectBlend<TBlack, TWhite>(TBlack black, TWhite white) : IHandler where TBlack : IHeightfieldFunction where TWhite : IHeightfieldFunction
        {
            public void Handle<T>(T newT, ref IHeightfieldFunction current, out IHandler next) where T : IHeightfieldFunction
            {
                current = new LerpFunction<TBlack, TWhite, T>(black, white, newT);
                next = null!;
            }
        }

        private struct Empty : IHeightfieldFunction
        {
            public void FillSamples(Span<Sample> samples) { }

            public void AppendTo(IFunctionCollector solution) { }
        }
    }

    private readonly record struct LerpFunction<TBlack, TWhite, TBlend>(TBlack Black, TWhite White, TBlend Blend) : IHeightfieldFunction
        where TBlack : IHeightfieldFunction
        where TWhite : IHeightfieldFunction
        where TBlend : IHeightfieldFunction
    {
        public unsafe void FillSamples(Span<Sample> samples)
        {
            Span<Sample> blends = stackalloc Sample[samples.Length];
            Blend.FillSamples(samples);
            samples.CopyTo(blends);

            int belowOne = 0;
            int aboveZero = 0;
            foreach (var blendFactor in blends)
            {
                if (blendFactor.Height > 0)
                    aboveZero++;
                if (blendFactor.Height < 1)
                    belowOne++;
            }

            if (aboveZero == 0)
            {
                Black.FillSamples(samples);
            }
            else if (belowOne == 0)
            {
                White.FillSamples(samples);
            }
            else if (aboveZero + belowOne != samples.Length * 2)
            {
                // The blend ranges do not all lay within {0, 1},
                // we can avoid sampling whites when the blend <= 0,
                // and blacks when blend >= 1.
                Span<Sample>
                    blacks = stackalloc Sample[belowOne],
                    whites = stackalloc Sample[aboveZero];
                int
                    blacksIndex = 0,
                    whitesIndex = 0;
                foreach (var blendSample in blends)
                {
                    if (blendSample.Height > 0)
                        whites[whitesIndex++] = blendSample;
                    if (blendSample.Height < 1)
                        blacks[blacksIndex++] = blendSample;
                }

                Black.FillSamples(blacks);
                White.FillSamples(whites);

                blacksIndex = 0;
                whitesIndex = 0;
                for (var i = 0; i < blends.Length; i++)
                {
                    var blendFactor = blends[i].Height;

                    float whiteHeight, blackHeight;
                    if (blendFactor < 1)
                        blackHeight = blacks[blacksIndex++].Height;
                    else
                        blackHeight = 0;

                    if (blendFactor > 0)
                        whiteHeight = whites[whitesIndex++].Height;
                    else
                        whiteHeight = 0;

                    // DETERMINISM: Result of ClampNative is architecture dependant, bepu does not offer cross-platform determinism right now, but this may change
                    //              The edge cases where this differs might not be observed when fed through lerp, to be validated
                    var t = float.ClampNative(blendFactor, 0, 1);
                    samples[i].Height = float.Lerp(whiteHeight, blackHeight, t);
                }
            }
            else
            {
                Span<Sample> blacks = stackalloc Sample[samples.Length];
                samples.CopyTo(blacks);
                Black.FillSamples(blacks);
                White.FillSamples(samples);
                for (int i = 0; i < samples.Length; i++)
                {
                    // DETERMINISM: Result of ClampNative is architecture dependant, bepu does not offer cross-platform determinism right now, but this may change
                    //              The edge cases where this differs might not be observed when fed through lerp, to be validated
                    var t = float.ClampNative(blends[i].Height, 0, 1);
                    samples[i].Height = float.Lerp(blacks[i].Height, samples[i].Height, t);
                }
            }
        }

        public void AppendTo(IFunctionCollector solution)
        {
            solution.Append(this);
        }
    }
}
