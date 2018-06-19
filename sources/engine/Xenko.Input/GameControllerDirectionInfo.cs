// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Input
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