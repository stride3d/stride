// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Provides some useful functions relating to game controllers
    /// </summary>
    public static class GameControllerUtils
    {
        /// <summary>
        /// Converts a <see cref="Direction"/> to a combination of <see cref="GamePadButton"/>'s
        /// </summary>
        public static GamePadButton DirectionToButtons(Direction direction)
        {
            GamePadButton buttonState = GamePadButton.None;
            if (direction.IsNeutral)
                return buttonState;

            int dPadValue = direction.GetTicks(8);
            switch (dPadValue)
            {
                case 0:
                    buttonState |= GamePadButton.PadUp;
                    break;
                case 1:
                    buttonState |= GamePadButton.PadUp;
                    buttonState |= GamePadButton.PadRight;
                    break;
                case 2:
                    buttonState |= GamePadButton.PadRight;
                    break;
                case 3:
                    buttonState |= GamePadButton.PadRight;
                    buttonState |= GamePadButton.PadDown;
                    break;
                case 4:
                    buttonState |= GamePadButton.PadDown;
                    break;
                case 5:
                    buttonState |= GamePadButton.PadDown;
                    buttonState |= GamePadButton.PadLeft;
                    break;
                case 6:
                    buttonState |= GamePadButton.PadLeft;
                    break;
                case 7:
                    buttonState |= GamePadButton.PadLeft;
                    buttonState |= GamePadButton.PadUp;
                    break;
            }
            return buttonState;
        }

        /// <summary>
        /// Converts the pad buttons of a <see cref="GamePadButton"/> to a <see cref="Direction"/>
        /// </summary>
        public static Direction ButtonsToDirection(GamePadButton padDirection)
        {
            int ticks = ButtonToTicks(padDirection);
            if (ticks < 0)
                return Direction.None;

            return Direction.FromTicks(ticks, 8);
        }

        private static int ButtonToTicks(GamePadButton padDirection)
        {
            switch (padDirection)
            {
                case GamePadButton.PadUp:
                    return 0;
                case GamePadButton.PadUp | GamePadButton.PadRight:
                    return 1;
                case GamePadButton.PadRight:
                    return 2;
                case GamePadButton.PadRight | GamePadButton.PadDown:
                    return 3;
                case GamePadButton.PadDown:
                    return 4;
                case GamePadButton.PadDown | GamePadButton.PadLeft:
                    return 5;
                case GamePadButton.PadLeft:
                    return 6;
                case GamePadButton.PadLeft | GamePadButton.PadUp:
                    return 7;
                default:
                    return -1;
            }
        }
    }
}