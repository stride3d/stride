// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine.Events;

namespace JumpyJet
{
    static class GameGlobals
    {
        public static EventKey GameOverEventKey = new EventKey("Global", "Game Over");
        public static EventKey GameStartedEventKey = new EventKey("Global", "Game Started");
        public static EventKey GameResetEventKey = new EventKey("Global", "Game Reset");
        public static EventKey PipePassedEventKey = new EventKey("Global", "Pipe Passed");

        public const float GameSpeed = 2.90f;
        public const float GamePixelToUnitScale = 0.01f;
    }
}
