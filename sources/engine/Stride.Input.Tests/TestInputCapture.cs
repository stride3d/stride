// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers capturing and releasing input devices and pointers through <see cref="InputManager"/>.
/// </summary>
public class TestInputCapture
{
    [Fact]
    public void CaptureSucceedsOnAFreeDevice()
    {
        using var headless = new HeadlessInput();
        var owner = new object();

        Assert.True(headless.Input.TryCapture(headless.Keyboard, owner));
        Assert.Same(owner, headless.Keyboard.CaptureState.Owner);
    }

    [Fact]
    public void EqualPriorityDoesNotTakeTheDevice()
    {
        using var headless = new HeadlessInput();
        var holder = new object();
        var challenger = new object();

        headless.Input.TryCapture(headless.Keyboard, holder, InputCapturePriority.Focus);

        Assert.False(headless.Input.TryCapture(headless.Keyboard, challenger, InputCapturePriority.Focus));
        Assert.Same(holder, headless.Keyboard.CaptureState.Owner);
    }

    [Fact]
    public void HigherPriorityTakesTheDeviceAndNotifies()
    {
        using var headless = new HeadlessInput();
        var holder = new object();
        var challenger = new object();
        DeviceCaptureChangedEventArgs seen = null;
        headless.Input.CaptureChanged += (_, e) => seen = e;

        headless.Input.TryCapture(headless.Keyboard, holder, InputCapturePriority.Focus);

        Assert.True(headless.Input.TryCapture(headless.Keyboard, challenger, InputCapturePriority.Modal));
        Assert.Same(headless.Keyboard, seen.Device);
        Assert.Same(holder, seen.PreviousOwner);
        Assert.Same(challenger, seen.NewOwner);
        Assert.Null(seen.PointerId);
    }

    [Fact]
    public void ReleaseByANonOwnerDoesNothing()
    {
        using var headless = new HeadlessInput();
        var holder = new object();
        headless.Input.TryCapture(headless.Keyboard, holder);

        headless.Input.Release(headless.Keyboard, new object());

        Assert.Same(holder, headless.Keyboard.CaptureState.Owner);
    }

    [Fact]
    public void PointerCaptureFollowsTheSamePriorityRules()
    {
        using var headless = new HeadlessInput();
        var holder = new object();

        Assert.True(headless.Input.TryCapturePointer(headless.Mouse, 1, holder));
        Assert.False(headless.Input.TryCapturePointer(headless.Mouse, 1, new object()));
        Assert.Same(holder, headless.Mouse.CaptureState.GetPointerOwner(1));
        Assert.False(headless.Mouse.CaptureState.IsCaptured);
    }

    [Fact]
    public void ReleaseAllReleasesDevicesAndPointersOfTheOwner()
    {
        using var headless = new HeadlessInput();
        var owner = new object();
        headless.Input.TryCapture(headless.Keyboard, owner);
        headless.Input.TryCapturePointer(headless.Mouse, 2, owner);

        headless.Input.ReleaseAll(owner);

        Assert.False(headless.Keyboard.CaptureState.IsCaptured);
        Assert.Null(headless.Mouse.CaptureState.GetPointerOwner(2));
    }

    [Fact]
    public void MaskingToggleAppliesToCapturedAndLaterDevices()
    {
        using var headless = new HeadlessInput();
        headless.Input.TryCapture(headless.Keyboard, new object());

        headless.Input.MaskCapturedInput = false;
        Assert.False(headless.Keyboard.CaptureState.IsMasked);

        var source = new InputSourceSimulated();
        headless.Input.Sources.Add(source);
        var laterKeyboard = source.AddKeyboard();
        headless.Input.TryCapture(laterKeyboard, new object());

        Assert.False(laterKeyboard.CaptureState.IsMasked);
    }

    [Fact]
    public void RemovingACapturedDeviceDropsItsCapture()
    {
        using var headless = new HeadlessInput();
        var source = new InputSourceSimulated();
        headless.Input.Sources.Add(source);
        var gamePad = source.AddGamePad();
        var owner = new object();
        headless.Input.TryCapture(gamePad, owner);
        DeviceCaptureChangedEventArgs seen = null;
        headless.Input.CaptureChanged += (_, e) => seen = e;

        source.RemoveGamePad(gamePad);

        Assert.False(gamePad.CaptureState.IsCaptured);
        Assert.Same(owner, seen.PreviousOwner);
        Assert.Null(seen.NewOwner);
    }
}
