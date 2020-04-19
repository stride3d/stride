// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// An event to describe a change in a game controller axis state
    /// </summary>
    public class GameControllerAxisEvent : AxisEvent
    {
        /// <summary>
        /// Index of the axis
        /// </summary>
        public int Index;

        /// <summary>
        /// The game controller that sent this event
        /// </summary>
        public IGameControllerDevice GameController => (IGameControllerDevice)Device;

        public override string ToString()
        {
            return $"{nameof(Index)}: {Index} ({GameController.AxisInfos[Index].Name}), {nameof(Value)}: {Value}, {nameof(GameController)}: {GameController.Name}";
        }
    }
}