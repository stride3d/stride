// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics.Tests;

/// <summary>
///   The allocation and collection cost of a measured workload.
/// </summary>
/// <param name="AllocatedBytes">Bytes allocated across all measured iterations.</param>
/// <param name="Gen0Collections">Generation 0 collections that ran during the measurement.</param>
/// <param name="Gen1Collections">Generation 1 collections that ran during the measurement.</param>
/// <param name="Gen2Collections">Generation 2 collections that ran during the measurement.</param>
/// <param name="Iterations">The number of measured iterations.</param>
internal readonly record struct GCMeasurement(
    long AllocatedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    int Iterations)
{
    /// <summary>
    ///   Gets the mean bytes allocated per iteration.
    /// </summary>
    public double BytesPerIteration => Iterations == 0 ? 0d : (double) AllocatedBytes / Iterations;

    public override string ToString() =>
        $"{AllocatedBytes:N0} bytes over {Iterations:N0} iterations ({BytesPerIteration:N1} per iteration), " +
        $"GC gen0={Gen0Collections} gen1={Gen1Collections} gen2={Gen2Collections}";
}

/// <summary>
///   Measures the allocation and collection cost of a workload.
/// </summary>
/// <remarks>
///   <para>
///     Allocation rate and collection cost are separate axes. Allocated bytes decide how often
///     generation 0 runs. Collection counts decide what the workload actually costs, and a
///     workload that allocates nothing can still make collections more expensive by writing
///     references the collector has to trace. Measure both.
///   </para>
///   <para>
///     Every measurement runs a warm-up first. Pools, and the capacity of any list or dictionary
///     the workload touches, grow on first use. Without a warm-up those first-touch allocations
///     are indistinguishable from a steady-state leak.
///   </para>
/// </remarks>
internal static class GCMeasure
{
    /// <summary>
    ///   Measures a workload that runs entirely on the calling thread.
    /// </summary>
    /// <param name="iteration">The workload. Invoked once per iteration.</param>
    /// <param name="warmupIterations">Iterations to run and discard before measuring.</param>
    /// <param name="measuredIterations">Iterations to measure.</param>
    /// <remarks>
    ///   Reads <see cref="GC.GetAllocatedBytesForCurrentThread"/>, which ignores allocations made
    ///   on any other thread. Use <see cref="RunAcrossThreads"/> when the workload dispatches work
    ///   to workers, as the render path does.
    /// </remarks>
    public static GCMeasurement Run(Action iteration, int warmupIterations = 64, int measuredIterations = 256)
        => Measure(iteration, warmupIterations, measuredIterations, GC.GetAllocatedBytesForCurrentThread);

    /// <summary>
    ///   Measures a workload that may allocate on threads other than the caller.
    /// </summary>
    /// <param name="iteration">The workload. Invoked once per iteration.</param>
    /// <param name="warmupIterations">Iterations to run and discard before measuring.</param>
    /// <param name="measuredIterations">Iterations to measure.</param>
    /// <remarks>
    ///   Reads <see cref="GC.GetTotalAllocatedBytes(bool)"/> precisely, which counts the whole
    ///   process. Anything else allocating at the same time lands in the result, so this is only
    ///   meaningful on an otherwise quiet process.
    /// </remarks>
    public static GCMeasurement RunAcrossThreads(Action iteration, int warmupIterations = 64, int measuredIterations = 256)
        => Measure(iteration, warmupIterations, measuredIterations, static () => GC.GetTotalAllocatedBytes(precise: true));

    private static GCMeasurement Measure(Action iteration, int warmupIterations, int measuredIterations, Func<long> readAllocatedBytes)
    {
        ArgumentNullException.ThrowIfNull(iteration);
        ArgumentOutOfRangeException.ThrowIfNegative(warmupIterations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(measuredIterations);

        for (int i = 0; i < warmupIterations; i++)
            iteration();

        // Settle anything the warm-up left behind, so collections counted below belong to the
        // measured iterations rather than to the warm-up.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var bytesBefore = readAllocatedBytes();
        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);

        for (int i = 0; i < measuredIterations; i++)
            iteration();

        return new GCMeasurement(
            readAllocatedBytes() - bytesBefore,
            GC.CollectionCount(0) - gen0Before,
            GC.CollectionCount(1) - gen1Before,
            GC.CollectionCount(2) - gen2Before,
            measuredIterations);
    }
}
