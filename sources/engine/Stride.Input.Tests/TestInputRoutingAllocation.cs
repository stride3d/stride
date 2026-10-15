// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Measures what routing input events to listeners costs the garbage collector.
/// </summary>
/// <remarks>
///   Every input event is routed to every listener of its type, every frame. Anything routing
///   allocates is allocated again for each event, so the cost grows with how much input arrives.
///   The measurement routes a fixed batch of events and nothing else, so what devices and the rest
///   of the input frame allocate, which differs between runtimes, stays out of it. Each iteration
///   resets the input manager's per-frame state first, as a frame does, since the input manager
///   listens to the events it routes and keeps them until the next frame.
/// </remarks>
public class TestInputRoutingAllocation
{
    private const int KeysPerBatch = 16;
    private const int MouseMovesPerBatch = 8;
    private const int Listeners = 4;

    [Fact]
    public void RoutingInputEventsDoesNotAllocate()
    {
        using var headless = new HeadlessInput();
        for (int i = 0; i < Listeners; i++)
            headless.Input.AddListener(new NullListener());

        var events = CreateEvents(headless);

        var measurement = GCMeasure.Run(() =>
        {
            headless.Input.ResetGlobalInputState();
            headless.Input.RouteEvents(events);
        });

        Assert.True(
            measurement.AllocatedBytes == 0,
            $"Routing {events.Count} events to {Listeners} listeners allocated. Measured {measurement}.");
    }

    private static List<InputEvent> CreateEvents(HeadlessInput headless)
    {
        var events = new List<InputEvent>();

        for (int i = 0; i < KeysPerBatch; i++)
        {
            events.Add(new KeyEvent { Device = headless.Keyboard, Key = Keys.A + i, IsDown = true });
            events.Add(new KeyEvent { Device = headless.Keyboard, Key = Keys.A + i, IsDown = false });
        }

        for (int i = 0; i < MouseMovesPerBatch; i++)
        {
            events.Add(new PointerEvent
            {
                Device = headless.Mouse,
                EventType = PointerEventType.Moved,
                Position = new Vector2(i / (float)MouseMovesPerBatch, 0.5f),
            });
        }

        return events;
    }

    private sealed class NullListener : IInputEventListener<KeyEvent>, IInputEventListener<PointerEvent>
    {
        public void ProcessEvent(KeyEvent inputEvent) { }

        public void ProcessEvent(PointerEvent inputEvent) { }
    }
}
