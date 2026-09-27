// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers what the game sees of a keyboard that is captured.
/// </summary>
public class TestKeyboardCapture
{
    [Fact]
    public void CapturedKeyboardReportsNothingDown()
    {
        using var headless = new HeadlessInput();
        headless.Keyboard.SimulateDown(Keys.W);
        headless.Update();

        headless.Input.TryCapture(headless.Keyboard, new object());

        Assert.Empty(headless.Keyboard.DownKeys);
        Assert.False(headless.Input.IsKeyDown(Keys.W));
    }

    [Fact]
    public void CaptureBeginShowsHeldKeysReleasedThenNothing()
    {
        using var headless = new HeadlessInput();
        headless.Keyboard.SimulateDown(Keys.W);
        headless.Update();

        headless.Input.TryCapture(headless.Keyboard, new object());
        Assert.True(headless.Input.IsKeyReleased(Keys.W));

        headless.Update();
        Assert.False(headless.Input.IsKeyReleased(Keys.W));
    }

    [Fact]
    public void KeyPressedInTheFrameACapturePhaseCaptureBeginsIsNeverSeen()
    {
        using var headless = new HeadlessInput();
        var owner = new object();
        headless.Input.ResolvingCapture += (_, _) => headless.Input.TryCapture(headless.Keyboard, owner);

        headless.Keyboard.SimulateDown(Keys.W);
        headless.Update();

        Assert.False(headless.Input.IsKeyPressed(Keys.W));
        Assert.False(headless.Input.IsKeyReleased(Keys.W));
    }

    [Fact]
    public void ReleaseInTheCapturePhaseKeepsAPressFromThatFrame()
    {
        using var headless = new HeadlessInput();
        var owner = new object();
        headless.Input.TryCapture(headless.Keyboard, owner);
        headless.Update();
        headless.Input.ResolvingCapture += (_, _) => headless.Input.Release(headless.Keyboard, owner);

        headless.Keyboard.SimulateDown(Keys.W);
        headless.Update();

        Assert.True(headless.Input.IsKeyPressed(Keys.W));
        Assert.True(headless.Input.IsKeyDown(Keys.W));
    }

    [Fact]
    public void KeyRepeatAfterCaptureEndsIsNotANewPress()
    {
        using var headless = new HeadlessInput();
        var owner = new object();
        headless.Input.TryCapture(headless.Keyboard, owner);
        headless.Keyboard.SimulateDown(Keys.W);
        headless.Update();

        headless.Input.Release(headless.Keyboard, owner);
        headless.Keyboard.SimulateDown(Keys.W); // operating-system key repeat while W is still held
        headless.Update();

        Assert.False(headless.Input.IsKeyDown(Keys.W));
        Assert.False(headless.Input.IsKeyPressed(Keys.W));
    }

    [Fact]
    public void KeyHeldWhenCaptureEndsNeedsANewPress()
    {
        using var headless = new HeadlessInput();
        var owner = new object();
        headless.Input.TryCapture(headless.Keyboard, owner);
        headless.Keyboard.SimulateDown(Keys.W);
        headless.Update();

        headless.Input.Release(headless.Keyboard, owner);
        headless.Update();
        Assert.False(headless.Input.IsKeyDown(Keys.W));

        headless.Keyboard.SimulateUp(Keys.W);
        headless.Update();
        Assert.False(headless.Input.IsKeyReleased(Keys.W));

        headless.Keyboard.SimulateDown(Keys.W);
        headless.Update();
        Assert.True(headless.Input.IsKeyPressed(Keys.W));
        Assert.True(headless.Input.IsKeyDown(Keys.W));
    }
}
