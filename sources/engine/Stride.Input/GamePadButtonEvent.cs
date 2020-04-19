// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// An event to describe a change in gamepad button state
    /// </summary>
    public class GamePadButtonEvent : ButtonEvent
    {
        /// <summary>
        /// The gamepad button identifier
        /// </summary>
        public GamePadButton Button;

        /// <summary>
        /// The gamepad that sent this event
        /// </summary>
        public IGamePadDevice GamePad => (IGamePadDevice)Device;

        public override string ToString()
        {
            return $"{nameof(Button)}: {Button}, {nameof(IsDown)}: {IsDown}, {nameof(GamePad)}: {GamePad.Name}";
        }
    }
}