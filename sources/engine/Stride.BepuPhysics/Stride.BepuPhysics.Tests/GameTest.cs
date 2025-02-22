// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Graphics.Regression;

namespace Stride.BepuPhysics.Tests
{
    /// <summary>
    /// Game class for test on the Input sensors.
    /// </summary>
    public class GameTest : GameTestBase
    {
        public GameTest()
        {
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
            // This still doesn't work IsDrawDesynchronized = false; // Double negation!
            TargetElapsedTime = TimeSpan.FromTicks(10000000 / 60); // target elapsed time is by default 60Hz
        }
    }
}

