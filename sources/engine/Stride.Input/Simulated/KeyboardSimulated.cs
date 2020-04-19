// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input
{
    public class KeyboardSimulated : KeyboardDeviceBase
    {
        public KeyboardSimulated(InputSourceSimulated source)
        {
            Priority = -1000;
            Source = source;
        }

        public override string Name => "Simulated Keyboard";

        public override Guid Id => new Guid(10, 10, 1, 0, 0, 0, 0, 0, 0, 0, 0);

        public override IInputSource Source { get; }

        public void SimulateDown(Keys key)
        {
            HandleKeyDown(key);
        }

        public void SimulateUp(Keys key)
        {
            HandleKeyUp(key);
        }
    }
}