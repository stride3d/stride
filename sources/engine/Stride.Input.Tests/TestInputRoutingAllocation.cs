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
/// </remarks>
public class TestInputRoutingAllocation
{
    private const int KeysPerFrame = 16;
    private const int MouseMovesPerFrame = 8;

    [Fact]
    public void RoutingInputEventsDoesNotAllocate()
    {
        using var headless = new HeadlessInput();
        headless.Input.AddListener(new NullListener());

        var measurement = GCMeasure.Run(() => RunOneFrameOfInput(headless), warmupIterations: 16, measuredIterations: 64);

        Assert.True(
            measurement.AllocatedBytes == 0,
            $"Routing {KeysPerFrame * 2} key events and {MouseMovesPerFrame} mouse moves per frame allocated. Measured {measurement}.");
    }

    private static void RunOneFrameOfInput(HeadlessInput headless)
    {
        for (int i = 0; i < KeysPerFrame; i++)
        {
            headless.Keyboard.SimulateDown(Keys.A + i);
            headless.Keyboard.SimulateUp(Keys.A + i);
        }

        for (int i = 0; i < MouseMovesPerFrame; i++)
            headless.Mouse.SetPosition(new Vector2(i / (float)MouseMovesPerFrame, 0.5f));

        headless.Update();
    }

    private sealed class NullListener : IInputEventListener<KeyEvent>, IInputEventListener<PointerEvent>
    {
        public void ProcessEvent(KeyEvent inputEvent) { }

        public void ProcessEvent(PointerEvent inputEvent) { }
    }
}
