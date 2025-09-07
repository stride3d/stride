// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Input;

namespace VRSandbox.Core;

public static class InputManagerExtensions
{
    public static bool IsGamePadButtonDown(this InputManager input, GamePadButton button, int index)
    {
        var gamepad = input.GetGamePadByIndex(index);
        if (gamepad == null)
            return false;

        return (gamepad.State.Buttons & button) == button;
    }

    public static Vector2 GetLeftThumb(this InputManager input, int index)
    {
        var gamepad = input.GetGamePadByIndex(index);
        return gamepad == null ? Vector2.Zero : gamepad.State.LeftThumb;
    }

    public static Vector2 GetRightThumb(this InputManager input, int index)
    {
        var gamepad = input.GetGamePadByIndex(index);
        return gamepad == null ? Vector2.Zero : gamepad.State.RightThumb;
    }

    public static float GetLeftTrigger(this InputManager input, int index)
    {
        var gamepad = input.GetGamePadByIndex(index);
        return gamepad == null ? 0.0f : gamepad.State.LeftTrigger;
    }

    public static float GetRightTrigger(this InputManager input, int index)
    {
        var gamepad = input.GetGamePadByIndex(index);
        return gamepad == null ? 0.0f : gamepad.State.RightTrigger;
    }
}
