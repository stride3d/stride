// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers what the game sees of a captured mouse, and of a pointer device with one captured pointer.
/// </summary>
public class TestPointerCapture
{
    [Fact]
    public void CapturedMouseHasNoButtonsOrDeltaButKeepsItsPosition()
    {
        using var headless = new HeadlessInput();
        headless.Mouse.SimulateMouseDown(MouseButton.Left);
        headless.Mouse.SetPosition(new Vector2(0.3f));
        headless.Update();

        headless.Input.TryCapture(headless.Mouse, new object());

        Assert.Empty(headless.Mouse.DownButtons);
        Assert.Equal(Vector2.Zero, headless.Mouse.Delta);
        Assert.True(headless.Mouse.IsButtonReleased(MouseButton.Left));
        Assert.Equal(new Vector2(0.3f), headless.Mouse.Position);
    }

    [Fact]
    public void CapturedMouseHidesItsPointer()
    {
        using var headless = new HeadlessInput();
        headless.Mouse.SimulateMouseDown(MouseButton.Left);
        headless.Update();

        headless.Input.TryCapture(headless.Mouse, new object());

        Assert.Empty(headless.Mouse.DownPointers);
    }

    [Fact]
    public void CapturingOnePointerLeavesTheOtherVisible()
    {
        using var headless = new HeadlessInput();
        var source = new InputSourceSimulated();
        headless.Input.Sources.Add(source);
        var touch = source.AddPointer();
        touch.SimulatePointer(PointerEventType.Pressed, new Vector2(0.1f), 0);
        touch.SimulatePointer(PointerEventType.Pressed, new Vector2(0.9f), 1);
        headless.Update();

        headless.Input.TryCapturePointer(touch, 0, new object());

        Assert.Single(touch.DownPointers);
        Assert.Equal(1, touch.DownPointers.First().Id);
    }
}
