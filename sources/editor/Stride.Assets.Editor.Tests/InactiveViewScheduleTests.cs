// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The views of a scene editor other than the active one are drawn at a fixed rate lower than the game's.
/// </summary>
public sealed class InactiveViewScheduleTests
{
    [Theory]
    [InlineData(60)]
    [InlineData(144)]
    [InlineData(30)]
    public void TheInactiveViewsAreDrawnAtTheLowerRateWhateverTheRateOfTheGame(double gameFramesPerSecond)
    {
        var schedule = new InactiveViewSchedule();

        var draws = CountDraws(schedule, gameFramesPerSecond, TimeSpan.FromSeconds(10));

        // About the lower rate, one draw more or less
        Assert.InRange(draws, InactiveViewSchedule.FramesPerSecond * 10 - 1, InactiveViewSchedule.FramesPerSecond * 10 + 1);
        Assert.InRange(InactiveViewSchedule.FramesPerSecond, 15, 20);
    }

    [Fact]
    public void AGameSlowerThanTheLowerRateDrawsTheInactiveViewsAtEachFrame()
    {
        var schedule = new InactiveViewSchedule();

        Assert.Equal(100, CountDraws(schedule, 10, TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void AfterAPauseTheScheduleStartsAgainFromTheNextFrame()
    {
        var schedule = new InactiveViewSchedule();
        Assert.True(schedule.IsDue(TimeSpan.Zero));
        Assert.False(schedule.IsDue(TimeSpan.FromMilliseconds(16)));

        // The game drew nothing for a second (hidden editor): the next frame draws, and the rhythm follows it
        Assert.True(schedule.IsDue(TimeSpan.FromSeconds(1)));
        Assert.False(schedule.IsDue(TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(16)));
        Assert.True(schedule.IsDue(TimeSpan.FromSeconds(1) + InactiveViewSchedule.Interval));
    }

    private static int CountDraws(InactiveViewSchedule schedule, double gameFramesPerSecond, TimeSpan duration)
    {
        var draws = 0;
        var frames = (int)(duration.TotalSeconds * gameFramesPerSecond);
        for (var frame = 0; frame < frames; frame++)
        {
            // The frame times of a game are rounded to ticks
            if (schedule.IsDue(TimeSpan.FromTicks((long)(frame * TimeSpan.TicksPerSecond / gameFramesPerSecond))))
                draws++;
        }
        return draws;
    }
}
