// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input.Tests;

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
///   <para>
///     The Mono interpreter optimizes a method once its calls and loop passes go over 1000, and that
///     can allocate on the calling thread. The counts are shared with the tests that ran before, so
///     the default warm-up runs more than 1000 iterations: every method the workload calls in each
///     iteration is then optimized before the measured iterations start.
///   </para>
///   <para>
///     A one-off allocation can still land in the measured iterations, for example from a method the
///     workload calls only sometimes. So a measurement that allocates is repeated and the lowest
///     result is kept. A one-off does not come back, and an allocation the workload makes every time does.
///   </para>
/// </remarks>
internal static class GCMeasure
{
    /// <summary>
    ///   The count of calls and loop passes past which the Mono interpreter optimizes a method (<c>INTERP_TIER_ENTRY_LIMIT</c>).
    /// </summary>
    private const int InterpreterTierUpLimit = 1000;

    /// <summary>
    ///   Measures a workload that runs entirely on the calling thread.
    /// </summary>
    /// <param name="iteration">The workload. Invoked once per iteration.</param>
    /// <param name="warmupIterations">Iterations to run and discard before measuring.</param>
    /// <param name="measuredIterations">Iterations to measure.</param>
    /// <param name="maxAttempts">How many times to measure, stopping early at a measurement with no allocation.</param>
    /// <remarks>
    ///   Reads <see cref="GC.GetAllocatedBytesForCurrentThread"/>, which ignores allocations made
    ///   on any other thread.
    /// </remarks>
    public static GCMeasurement Run(Action iteration, int warmupIterations = InterpreterTierUpLimit + 1, int measuredIterations = 256, int maxAttempts = 3)
    {
        ArgumentNullException.ThrowIfNull(iteration);
        ArgumentOutOfRangeException.ThrowIfNegative(warmupIterations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(measuredIterations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        Repeat(iteration, warmupIterations);

        var lowest = MeasureOnce(iteration, measuredIterations);
        for (int attempt = 1; attempt < maxAttempts && lowest.AllocatedBytes > 0; attempt++)
        {
            var measurement = MeasureOnce(iteration, measuredIterations);
            if (measurement.AllocatedBytes < lowest.AllocatedBytes)
                lowest = measurement;
        }

        return lowest;
    }

    private static GCMeasurement MeasureOnce(Action iteration, int measuredIterations)
    {
        // Settle anything the warm-up left behind, so collections counted below belong to the
        // measured iterations rather than to the warm-up.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);

        Repeat(iteration, measuredIterations);

        return new GCMeasurement(
            GC.GetAllocatedBytesForCurrentThread() - bytesBefore,
            GC.CollectionCount(0) - gen0Before,
            GC.CollectionCount(1) - gen1Before,
            GC.CollectionCount(2) - gen2Before,
            measuredIterations);
    }

    // One loop for the warm-up and the measured iterations, so the warm-up also gets the loop itself optimized
    private static void Repeat(Action iteration, int count)
    {
        for (int i = 0; i < count; i++)
            iteration();
    }
}
