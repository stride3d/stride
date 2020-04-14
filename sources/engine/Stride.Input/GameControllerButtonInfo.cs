// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Provides information about a gamepad button
    /// </summary>
    public class GameControllerButtonInfo : GameControllerObjectInfo
    {
        /// <summary>
        /// The type of button
        /// </summary>
        public GameControllerButtonType Type;

        public override string ToString()
        {
            return $"GameController Button {{{Name}}} [{Type}]";
        }
    }
}