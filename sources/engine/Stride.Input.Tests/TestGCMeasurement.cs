// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Checks that <see cref="GCMeasure"/> ignores a one-off allocation and still reports a repeated one.
/// </summary>
public class TestGCMeasurement
{
    private object kept;

    [Fact]
    public void OneOffAllocationInTheMeasuredIterationsIsNotReported()
    {
        const int warmupIterations = 4;
        int calls = 0;

        var measurement = GCMeasure.Run(() =>
        {
            if (++calls == warmupIterations + 1)
                kept = new byte[256];
        }, warmupIterations);

        Assert.True(measurement.AllocatedBytes == 0, $"Measured {measurement}.");
    }

    [Fact]
    public void AllocationInEveryIterationIsReported()
    {
        var measurement = GCMeasure.Run(() => kept = new byte[256]);

        Assert.True(measurement.BytesPerIteration >= 256, $"Measured {measurement}.");
    }
}
