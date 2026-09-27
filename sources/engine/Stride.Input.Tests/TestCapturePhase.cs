// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers the capture phase of <see cref="InputManager.Update"/> and the game-facing state built after it.
/// </summary>
public class TestCapturePhase
{
    [Fact]
    public void CaptureInTheCapturePhaseHidesThisFramesMouseInput()
    {
        using var headless = new HeadlessInput();
        var owner = new object();
        headless.Input.ResolvingCapture += (_, _) => headless.Input.TryCapture(headless.Mouse, owner);

        headless.Mouse.SetPosition(new Vector2(0.5f));
        headless.Mouse.SimulateMouseWheel(1f);
        headless.Update();

        Assert.Equal(Vector2.Zero, headless.Input.MouseDelta);
        Assert.Equal(0f, headless.Input.MouseWheelDelta);
        Assert.Empty(headless.Input.PointerEvents);
    }

    [Fact]
    public void CaptureAfterTheCapturePhaseMasksEventListsOnNextRead()
    {
        using var headless = new HeadlessInput();
        headless.Keyboard.SimulateDown(Keys.A);
        headless.Update();
        Assert.Single(headless.Input.KeyEvents);

        headless.Input.TryCapture(headless.Keyboard, new object());

        Assert.Empty(headless.Input.KeyEvents);
        Assert.Empty(headless.Input.Events.OfType<KeyEvent>());
    }

    [Fact]
    public void GesturesSeeUncapturedPointers()
    {
        using var headless = new HeadlessInput();
        var touch = AddTouch(headless);
        headless.Input.Gestures.Add(new GestureConfigDrag());

        Drag(headless, touch);

        Assert.NotEmpty(headless.Input.GestureEvents);
    }

    [Fact]
    public void GesturesDoNotSeeCapturedPointers()
    {
        using var headless = new HeadlessInput();
        var touch = AddTouch(headless);
        headless.Input.Gestures.Add(new GestureConfigDrag());
        headless.Input.ResolvingCapture += (_, _) => headless.Input.TryCapturePointer(touch, 0, headless);

        Drag(headless, touch);

        Assert.Empty(headless.Input.GestureEvents);
    }

    [Fact]
    public void CapturePhaseAddsNoAllocationPerFrame()
    {
        // Compare against the same frame without a capture owner, so only what capture adds is measured.
        var withoutCapture = MeasureFrames(captureMouse: false);
        var withCapture = MeasureFrames(captureMouse: true);

        Assert.True(
            withCapture.AllocatedBytes == withoutCapture.AllocatedBytes,
            $"Capturing added allocations. Without capture: {withoutCapture}. With capture: {withCapture}.");
    }

    private static GCMeasurement MeasureFrames(bool captureMouse)
    {
        using var headless = new HeadlessInput();
        var owner = new object();
        if (captureMouse)
            headless.Input.ResolvingCapture += (_, _) => headless.Input.TryCapture(headless.Mouse, owner);

        return GCMeasure.Run(() =>
        {
            headless.Mouse.SetPosition(new Vector2(0.4f));
            headless.Mouse.SetPosition(new Vector2(0.6f));
            headless.Update();
            _ = headless.Input.PointerEvents.Count;
            _ = headless.Input.MouseDelta;
        }, warmupIterations: 16, measuredIterations: 64);
    }

    private static PointerSimulated AddTouch(HeadlessInput headless)
    {
        var source = new InputSourceSimulated();
        headless.Input.Sources.Add(source);
        return source.AddPointer();
    }

    private static void Drag(HeadlessInput headless, PointerSimulated touch)
    {
        touch.SimulatePointer(PointerEventType.Pressed, new Vector2(0.1f, 0.5f), 0);
        headless.Update();
        for (int step = 1; step <= 8; step++)
        {
            touch.SimulatePointer(PointerEventType.Moved, new Vector2(0.1f + step * 0.1f, 0.5f), 0);
            headless.Update();
        }
    }
}
