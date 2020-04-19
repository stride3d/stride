// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Input
{
    /// <summary>
    /// Keeps track of <see cref="GamePadLayout"/>
    /// </summary>
    public static class GamePadLayouts
    {
        private static readonly List<GamePadLayout> layouts = new List<GamePadLayout>();

        static GamePadLayouts()
        {
            // XInput device layout for any plaform that does not support xinput directly
            AddLayout(new GamePadLayoutXInput());

            // Support for DualShock4 controllers
            AddLayout(new GamePadLayoutDS4());
        }

        /// <summary>
        /// Adds a new layout that cane be used for mapping gamepads to <see cref="GamePadState"/>
        /// </summary>
        /// <param name="layout">The layout to add</param>
        public static void AddLayout(GamePadLayout layout)
        {
            lock (layouts)
            {
                if (!layouts.Contains(layout))
                {
                    layouts.Add(layout);
                }
            }
        }

        /// <summary>
        /// Finds a layout matching the given gamepad
        /// </summary>
        /// <param name="source">The source that the <paramref name="device"/> came from</param>
        /// <param name="device">The device to find a layout for</param>
        /// <returns>The gamepad layout that was found, or null if none was found</returns>
        public static GamePadLayout FindLayout(IInputSource source, IGameControllerDevice device)
        {
            lock (layouts)
            {
                foreach (var layout in layouts)
                {
                    if (layout.MatchDevice(source, device))
                        return layout;
                }
                return null;
            }
        }
    }
}