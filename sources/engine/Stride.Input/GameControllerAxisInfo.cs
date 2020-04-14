// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Provides information about a gamepad axis
    /// </summary>
    public class GameControllerAxisInfo : GameControllerObjectInfo
    {
        public override string ToString()
        {
            return $"GameController Axis {{{Name}}}";
        }
    }
}