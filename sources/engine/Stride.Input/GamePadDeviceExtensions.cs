// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type

namespace Stride.Input
{
    /// <summary>
    /// Provides easier ways to set vibration levels on a controller, rather than setting 4 motors
    /// </summary>
    public static class GamePadDeviceExtensions
    {
        /// <summary>
        /// Sets all the gamepad vibration motors to the same amount
        /// </summary>
        /// <param name="gamepad">The gamepad</param>
        /// <param name="amount">The amount of vibration</param>
        public static void SetVibration(this IGamePadDevice gamepad, float amount)
        {
            gamepad.SetVibration(amount, amount, amount, amount);
        }

        /// <summary>
        /// Sets the gamepad's large and small motors to the given amounts
        /// </summary>
        /// <param name="gamepad">The gamepad</param>
        /// <param name="largeMotors">The amount of vibration for the large motors</param>
        /// <param name="smallMotors">The amount of vibration for the small motors</param>
        public static void SetVibration(this IGamePadDevice gamepad, float largeMotors, float smallMotors)
        {
            gamepad.SetVibration(smallMotors, smallMotors, largeMotors, largeMotors);
        }

        /// <summary>
        /// Determines whether the specified button is pressed since the previous update.
        /// </summary>
        /// <param name="gamepad">The gamepad</param>
        /// <param name="button">The button</param>
        /// <returns><c>true</c> if the specified button is pressed; otherwise, <c>false</c>.</returns>
        public static bool IsButtonPressed(this IGamePadDevice gamepad, GamePadButton button)
        {
            return gamepad.PressedButtons.Contains(button);
        }

        /// <summary>
        /// Determines whether the specified button is released since the previous update.
        /// </summary>
        /// /// <param name="gamepad">The gamepad</param>
        /// <param name="button">The button</param>
        /// <returns><c>true</c> if the specified button is released; otherwise, <c>false</c>.</returns>
        public static bool IsButtonReleased(this IGamePadDevice gamepad, GamePadButton button)
        {
            return gamepad.ReleasedButtons.Contains(button);
        }

        /// <summary>
        /// Determines whether the specified button is being pressed down
        /// </summary>
        /// /// <param name="gamepad">The gamepad</param>
        /// <param name="button">The button</param>
        /// <returns><c>true</c> if the specified button is being pressed down; otherwise, <c>false</c>.</returns>
        public static bool IsButtonDown(this IGamePadDevice gamepad, GamePadButton button)
        {
            return gamepad.DownButtons.Contains(button);
        }
    }

    /// <summary>
    /// Provides easier ways to set vibration levels on a controller, rather than setting 4 motors
    /// </summary>
    public static class GameControllerDeviceExtensions
    {
        /// <summary>
        /// Determines whether the specified button is pressed since the previous update.
        /// </summary>
        /// <param name="controller">The controller</param>
        /// <param name="button">The button</param>
        /// <returns><c>true</c> if the specified button is pressed; otherwise, <c>false</c>.</returns>
        public static bool IsButtonPressed(this IGameControllerDevice controller, int button)
        {
            return controller.PressedButtons.Contains(button);
        }

        /// <summary>
        /// Determines whether the specified button is released since the previous update.
        /// </summary>
        /// /// <param name="controller">The controller</param>
        /// <param name="button">The button</param>
        /// <returns><c>true</c> if the specified button is released; otherwise, <c>false</c>.</returns>
        public static bool IsButtonReleased(this IGameControllerDevice controller, int button)
        {
            return controller.ReleasedButtons.Contains(button);
        }

        /// <summary>
        /// Determines whether the specified button is being pressed down
        /// </summary>
        /// /// <param name="controller">The controller</param>
        /// <param name="button">The button</param>
        /// <returns><c>true</c> if the specified button is being pressed down; otherwise, <c>false</c>.</returns>
        public static bool IsButtonDown(this IGameControllerDevice controller, int button)
        {
            return controller.DownButtons.Contains(button);
        }
    }
}
