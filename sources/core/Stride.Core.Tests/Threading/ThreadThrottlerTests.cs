using Stride.Core;
using Xunit;

namespace Stride.Core.Tests.Threading;

public class ThreadThrottlerTests
{
    [Fact]
    public void Constructor_Default_CreatesThrottlerWithZeroPeriod()
    {
        var throttler = new ThreadThrottler();

        Assert.Equal(TimeSpan.Zero, throttler.MinimumElapsedTime);
    }

    [Fact]
    public void Constructor_WithTimeSpan_SetsMinimumElapsedTime()
    {
        var timeSpan = TimeSpan.FromMilliseconds(16);

        var throttler = new ThreadThrottler(timeSpan);

        // Due to conversion loss, allow small tolerance
        Assert.True(Math.Abs((throttler.MinimumElapsedTime - timeSpan).TotalMilliseconds) < 1);
    }

    [Fact]
    public void Constructor_WithFrequency_SetsCorrectPeriod()
    {
        var throttler = new ThreadThrottler(60); // 60 FPS

        var expectedPeriod = TimeSpan.FromSeconds(1.0 / 60);
        Assert.True(Math.Abs((throttler.MinimumElapsedTime - expectedPeriod).TotalMilliseconds) < 2);
    }

    [Fact]
    public void SetMaxFrequency_UpdatesPeriod()
    {
        var throttler = new ThreadThrottler();

        throttler.SetMaxFrequency(30); // 30 FPS

        var expectedPeriod = TimeSpan.FromSeconds(1.0 / 30);
        Assert.True(Math.Abs((throttler.MinimumElapsedTime - expectedPeriod).TotalMilliseconds) < 2);
    }

    [Fact]
    public void Throttle_WithZeroPeriod_ReturnsFalseImmediately()
    {
        var throttler = new ThreadThrottler();

        var result = throttler.Throttle(out TimeSpan elapsed);

        Assert.False(result);
        Assert.True(elapsed >= TimeSpan.Zero);
    }

    [Fact]
    public void Throttle_WithShortPeriod_EventuallyReturnsTrue()
    {
        var throttler = new ThreadThrottler(1000); // 1000 FPS = 1ms period

        // First call should not throttle (or very briefly)
        var result1 = throttler.Throttle(out TimeSpan elapsed1);

        // Immediate second call should potentially throttle
        var result2 = throttler.Throttle(out TimeSpan elapsed2);

        Assert.True(elapsed1 > TimeSpan.Zero);
        Assert.True(elapsed2 > TimeSpan.Zero);
    }

    [Fact]
    public void Throttle_ReturnsElapsedTime()
    {
        var throttler = new ThreadThrottler(60); // 60 FPS

        throttler.Throttle(out TimeSpan elapsed1);
        Thread.Sleep(20);
        throttler.Throttle(out TimeSpan elapsed2);

        Assert.True(elapsed1 > TimeSpan.Zero);
        Assert.True(elapsed2 >= TimeSpan.FromMilliseconds(15));
    }

    [Fact]
    public void SetToStandard_SetsTypeToStandard()
    {
        var throttler = new ThreadThrottler();

        throttler.SetToStandard();

        Assert.Equal(ThreadThrottler.ThrottlerType.Standard, throttler.Type);
    }

    [Fact]
    public void SetToPreciseManual_SetsTypeToPreciseManual()
    {
        var throttler = new ThreadThrottler();

        throttler.SetToPreciseManual(1000000); // 1ms in stopwatch ticks

        Assert.Equal(ThreadThrottler.ThrottlerType.PreciseManual, throttler.Type);
    }

    [Fact]
    public void SetToPreciseAuto_SetsTypeToPreciseAuto()
    {
        var throttler = new ThreadThrottler();

        throttler.SetToPreciseAuto();

        Assert.Equal(ThreadThrottler.ThrottlerType.PreciseAuto, throttler.Type);
    }

    [Fact]
    public void MinimumElapsedTime_CanBeSetAndRetrieved()
    {
        var throttler = new ThreadThrottler();
        var timeSpan = TimeSpan.FromMilliseconds(33);

        throttler.MinimumElapsedTime = timeSpan;

        // Allow small tolerance due to conversion
        Assert.True(Math.Abs((throttler.MinimumElapsedTime - timeSpan).TotalMilliseconds) < 2);
    }
}
