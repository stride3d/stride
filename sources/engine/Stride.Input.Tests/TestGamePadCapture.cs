// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers what the game sees of a captured gamepad.
/// </summary>
public class TestGamePadCapture
{
    [Fact]
    public void CapturedGamePadStateIsNeutral()
    {
        using var headless = new HeadlessInput();
        var source = new InputSourceSimulated();
        headless.Input.Sources.Add(source);
        var gamePad = source.AddGamePad();
        gamePad.SetButton(GamePadButton.A, true);
        gamePad.SetAxis(GamePadAxis.LeftThumbX, 1f);
        headless.Update();
        Assert.Equal(1f, gamePad.State.LeftThumb.X);

        headless.Input.TryCapture(gamePad, new object());

        Assert.Equal(default, gamePad.State);
        Assert.False(gamePad.IsButtonDown(GamePadButton.A));
        Assert.True(gamePad.IsButtonReleased(GamePadButton.A));
    }

    [Fact]
    public void ButtonHeldThroughCaptureEndIsHiddenFromState()
    {
        using var headless = new HeadlessInput();
        var source = new InputSourceSimulated();
        headless.Input.Sources.Add(source);
        var gamePad = source.AddGamePad();
        var owner = new object();
        headless.Input.TryCapture(gamePad, owner);
        gamePad.SetButton(GamePadButton.A, true);
        headless.Update();

        headless.Input.Release(gamePad, owner);

        Assert.False(gamePad.State.Buttons.HasFlag(GamePadButton.A));
        Assert.False(gamePad.IsButtonDown(GamePadButton.A));
    }
}
