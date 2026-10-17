// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Collections;
using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers the rules for input that is held when a capture begins or ends.
/// </summary>
public class TestCaptureEdgeTracker
{
    [Fact]
    public void UncapturedViewsMatchRawSets()
    {
        var (tracker, down, pressed, _, _) = Create();
        down.Add(Keys.A);
        pressed.Add(Keys.A);

        Assert.Contains(Keys.A, tracker.Down);
        Assert.Contains(Keys.A, tracker.Pressed);
        Assert.Single(tracker.Down);
    }

    [Fact]
    public void CaptureBeginReportsHeldKeysAsReleasedForOneFrame()
    {
        var (tracker, down, _, _, capture) = Create();
        down.Add(Keys.W);

        capture.SetOwner(new object(), 0);

        Assert.Contains(Keys.W, tracker.Released);
        Assert.DoesNotContain(Keys.W, tracker.Down);

        tracker.AfterDeviceUpdate();

        Assert.Empty(tracker.Released);
        Assert.Empty(tracker.Down);
    }

    [Fact]
    public void CaptureBeginDoesNotReleaseKeysPressedThisFrame()
    {
        var (tracker, down, pressed, _, capture) = Create();
        down.Add(Keys.W);
        pressed.Add(Keys.W);
        capture.ChangingBeforeGameReads = true;

        capture.SetOwner(new object(), 0);

        Assert.DoesNotContain(Keys.W, tracker.Released);
    }

    [Fact]
    public void CaptureBeginReportsKeysReleasedThisFrame()
    {
        var (tracker, _, _, released, capture) = Create();
        released.Add(Keys.W);

        capture.SetOwner(new object(), 0);

        Assert.Contains(Keys.W, tracker.Released);
    }

    [Fact]
    public void RepeatOfASuppressedKeyIsNotPressed()
    {
        var down = new HashSet<Keys>();
        var pressed = new HashSet<Keys>();
        var newPresses = new HashSet<Keys>();
        var capture = DeviceCaptureStates.GetOrCreate(new ThirdPartyKeyboard());
        var tracker = new CaptureEdgeTracker<Keys>(capture, new ReadOnlySet<Keys>(down), new ReadOnlySet<Keys>(pressed), new ReadOnlySet<Keys>(new HashSet<Keys>()), new ReadOnlySet<Keys>(newPresses));
        capture.MaskChanged += tracker.OnMaskChanged;
        capture.SetOwner(new object(), 0);
        down.Add(Keys.W);
        capture.SetOwner(null, 0);

        pressed.Add(Keys.W); // a repeat: pressed, but not a new press
        tracker.AfterDeviceUpdate();

        Assert.DoesNotContain(Keys.W, tracker.Pressed);
        Assert.Empty(tracker.Pressed);
    }

    [Fact]
    public void CaptureEndInTheCapturePhaseKeepsAPressFromThatFrame()
    {
        var (tracker, down, pressed, _, capture) = Create();
        capture.SetOwner(new object(), 0);
        down.Add(Keys.W);
        pressed.Add(Keys.W);

        capture.ChangingBeforeGameReads = true;
        capture.SetOwner(null, 0);

        Assert.Contains(Keys.W, tracker.Down);
        Assert.Contains(Keys.W, tracker.Pressed);
    }

    [Fact]
    public void KeyHeldThroughCaptureEndIsHiddenUntilPressedAgain()
    {
        var (tracker, down, pressed, released, capture) = Create();
        capture.SetOwner(new object(), 0);
        down.Add(Keys.W);

        capture.SetOwner(null, 0);
        Assert.DoesNotContain(Keys.W, tracker.Down);

        down.Remove(Keys.W);
        released.Add(Keys.W);
        tracker.AfterDeviceUpdate();
        Assert.DoesNotContain(Keys.W, tracker.Released);

        released.Clear();
        down.Add(Keys.W);
        pressed.Add(Keys.W);
        tracker.AfterDeviceUpdate();
        Assert.Contains(Keys.W, tracker.Pressed);
        Assert.Contains(Keys.W, tracker.Down);
    }

    private static (CaptureEdgeTracker<Keys> Tracker, HashSet<Keys> Down, HashSet<Keys> Pressed, HashSet<Keys> Released, DeviceCaptureState Capture) Create()
    {
        var down = new HashSet<Keys>();
        var pressed = new HashSet<Keys>();
        var released = new HashSet<Keys>();
        var capture = DeviceCaptureStates.GetOrCreate(new ThirdPartyKeyboard());
        var tracker = new CaptureEdgeTracker<Keys>(capture, new ReadOnlySet<Keys>(down), new ReadOnlySet<Keys>(pressed), new ReadOnlySet<Keys>(released));
        capture.MaskChanged += tracker.OnMaskChanged;
        return (tracker, down, pressed, released, capture);
    }
}
