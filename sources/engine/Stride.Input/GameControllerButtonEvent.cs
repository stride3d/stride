// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// An event to describe a change in game controller button state
    /// </summary>
    public class GameControllerButtonEvent : ButtonEvent
    {
        /// <summary>
        /// The index of the button
        /// </summary>
        public int Index;

        /// <summary>
        /// The game controller that sent this event
        /// </summary>
        public IGameControllerDevice GameController => (IGameControllerDevice)Device;

        public override string ToString()
        {
            return $"{nameof(Index)}: {Index} ({GameController.ButtonInfos[Index].Name}), {nameof(IsDown)}: {IsDown}, {nameof(GameController)}: {GameController.Name}";
        }
    }
}