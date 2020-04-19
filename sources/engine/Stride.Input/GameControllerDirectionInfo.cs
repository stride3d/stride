// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Provides information about a gamepad direction input
    /// </summary>
    public class GameControllerDirectionInfo : GameControllerObjectInfo
    {
        public override string ToString()
        {
            return $"GameController Direction {{{Name}}}";
        }
    }
}