// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers masking through the shared query methods, which applies to every device, including devices that
///   implement the interfaces directly.
/// </summary>
public class TestQueryMasking
{
    [Fact]
    public void QueriesOnAThirdPartyKeyboardAreMaskedWhileCaptured()
    {
        using var headless = new HeadlessInput();
        var keyboard = new ThirdPartyKeyboard();
        keyboard.Down.Add(Keys.W);
        keyboard.Pressed.Add(Keys.W);

        headless.Input.TryCapture(keyboard, new object());

        Assert.False(keyboard.IsKeyDown(Keys.W));
        Assert.False(keyboard.IsKeyPressed(Keys.W));
    }

    [Fact]
    public void QueriesOnAThirdPartyKeyboardAreRawWhenMaskingIsOff()
    {
        using var headless = new HeadlessInput();
        var keyboard = new ThirdPartyKeyboard();
        keyboard.Down.Add(Keys.W);
        // The device is not registered with the manager, so masking is set on the device directly.
        ((IInputDevice)keyboard).CaptureState.SetMaskingEnabled(false);

        headless.Input.TryCapture(keyboard, new object());

        Assert.True(keyboard.IsKeyDown(Keys.W));
    }

    [Fact]
    public void QueriesOnAThirdPartyGamePadAreMaskedWhileCaptured()
    {
        using var headless = new HeadlessInput();
        var gamePad = new ThirdPartyGamePad();
        gamePad.Down.Add(GamePadButton.A);
        gamePad.Released.Add(GamePadButton.B);

        headless.Input.TryCapture(gamePad, new object());

        Assert.False(gamePad.IsButtonDown(GamePadButton.A));
        Assert.False(gamePad.IsButtonReleased(GamePadButton.B));
    }

    [Fact]
    public void QueriesOnAThirdPartyGamePadAreRawWhileNotCaptured()
    {
        var gamePad = new ThirdPartyGamePad();
        gamePad.Down.Add(GamePadButton.A);

        Assert.True(gamePad.IsButtonDown(GamePadButton.A));
    }
}
