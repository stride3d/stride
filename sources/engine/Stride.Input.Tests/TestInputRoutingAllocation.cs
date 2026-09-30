// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Measures what routing input events to listeners costs the garbage collector.
/// </summary>
/// <remarks>
///   Every input event is routed to every listener of its type, every frame. Anything routing
///   allocates is allocated again for each event, so the cost grows with how much input arrives.
///   The measurement compares a frame with input against the same frame without any, so a cost the
///   runtime adds to every frame regardless of input cancels out. On Mono (Android and iOS) the input
///   path keeps allocating for its first few dozen frames before it settles, so the warm-up runs well
///   past that.
/// </remarks>
public class TestInputRoutingAllocation
{
    private const int KeysPerFrame = 16;
    private const int MouseMovesPerFrame = 8;
    private const int WarmupFrames = 256;
    private const int MeasuredFrames = 256;

    [Fact]
    public void RoutingInputEventsDoesNotAllocate()
    {
        var withoutInput = MeasureFrames(withInput: false);
        var withInput = MeasureFrames(withInput: true);

        Assert.True(
            withInput.AllocatedBytes == withoutInput.AllocatedBytes,
            $"Routing {KeysPerFrame * 2} key events and {MouseMovesPerFrame} mouse moves per frame allocated. " +
            $"Without input: {withoutInput}. With input: {withInput}.");
    }

    private static GCMeasurement MeasureFrames(bool withInput)
    {
        using var headless = new HeadlessInput();
        headless.Input.AddListener(new NullListener());

        return GCMeasure.Run(() => RunOneFrame(headless, withInput), WarmupFrames, MeasuredFrames);
    }

    private static void RunOneFrame(HeadlessInput headless, bool withInput)
    {
        if (withInput)
        {
            for (int i = 0; i < KeysPerFrame; i++)
            {
                headless.Keyboard.SimulateDown(Keys.A + i);
                headless.Keyboard.SimulateUp(Keys.A + i);
            }

            for (int i = 0; i < MouseMovesPerFrame; i++)
                headless.Mouse.SetPosition(new Vector2(i / (float)MouseMovesPerFrame, 0.5f));
        }

        headless.Update();
    }

    private sealed class NullListener : IInputEventListener<KeyEvent>, IInputEventListener<PointerEvent>
    {
        public void ProcessEvent(KeyEvent inputEvent) { }

        public void ProcessEvent(PointerEvent inputEvent) { }
    }
}
