// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace Stride.Games
{
    /// <summary>
    /// Current timing used for variable-step (real time) or fixed-step (game time) games.
    /// </summary>
    public class GameTime
    {
        private TimeSpan accumulatedElapsedTime;
        private int accumulatedFrameCountPerSecond;
        private double factor;

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTime" /> class.
        /// </summary>
        public GameTime()
        {
            accumulatedElapsedTime = TimeSpan.Zero;
            factor = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTime" /> class.
        /// </summary>
        /// <param name="totalTime">The total game time since the start of the game.</param>
        /// <param name="elapsedTime">The elapsed game time since the last update.</param>
        public GameTime(TimeSpan totalTime, TimeSpan elapsedTime)
        {
            Total = totalTime;
            Elapsed = elapsedTime;
            accumulatedElapsedTime = TimeSpan.Zero;
            factor = 1;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the elapsed game time since the last update
        /// </summary>
        /// <value>The elapsed game time.</value>
        public TimeSpan Elapsed { get; private set; }

        /// <summary>
        /// Gets the amount of game time since the start of the game.
        /// </summary>
        /// <value>The total game time.</value>
        public TimeSpan Total { get; private set; }

        /// <summary>
        /// Gets the current frame count since the start of the game.
        /// </summary>
        public int FrameCount { get; private set; }

        /// <summary>
        /// Gets the number of frame per second (FPS) for the current running game.
        /// </summary>
        /// <value>The frame per second.</value>
        public float FramePerSecond { get; private set; }

        /// <summary>
        /// Gets the time per frame.
        /// </summary>
        /// <value>The time per frame.</value>
        public TimeSpan TimePerFrame { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="FramePerSecond"/> and <see cref="TimePerFrame"/> were updated for this frame.
        /// </summary>
        /// <value><c>true</c> if the <see cref="FramePerSecond"/> and <see cref="TimePerFrame"/> were updated for this frame; otherwise, <c>false</c>.</value>
        public bool FramePerSecondUpdated { get; private set; }



        /// <summary>
        /// Gets the amount of time elapsed multiplied by the time factor.
        /// </summary>
        /// <value>The warped elapsed time</value>
        public TimeSpan WarpElapsed { get; private set; }


        /// <summary>
        /// Gets or sets the time factor.<br/>
        /// This value controls how much the warped time flows, this includes physics, animations and particles.
        /// A value between 0 and 1 will slow time, a value above 1 will make it faster.
        /// </summary>
        /// <value>The multiply factor, a double value higher or equal to 0</value>
        public double Factor
        {
            get => factor;
            set => factor = value > 0 ? value : 0;
        }


        internal void Update(TimeSpan totalGameTime, TimeSpan elapsedGameTime, bool incrementFrameCount)
        {
            Total = totalGameTime;
            Elapsed = elapsedGameTime;
            WarpElapsed = TimeSpan.FromTicks((long)(Elapsed.Ticks * Factor));

            FramePerSecondUpdated = false;

            if (incrementFrameCount)
            {
                accumulatedElapsedTime += elapsedGameTime;
                var accumulatedElapsedGameTimeInSecond = accumulatedElapsedTime.TotalSeconds;
                if (accumulatedFrameCountPerSecond > 0 && accumulatedElapsedGameTimeInSecond > 1.0)
                {
                    TimePerFrame = TimeSpan.FromTicks(accumulatedElapsedTime.Ticks / accumulatedFrameCountPerSecond);
                    FramePerSecond = (float)(accumulatedFrameCountPerSecond / accumulatedElapsedGameTimeInSecond);
                    accumulatedFrameCountPerSecond = 0;
                    accumulatedElapsedTime = TimeSpan.Zero;
                    FramePerSecondUpdated = true;
                }

                accumulatedFrameCountPerSecond++;
                FrameCount++;
            }
        }

        internal void Reset(TimeSpan totalGameTime)
        {
            Update(totalGameTime, TimeSpan.Zero, false);
            accumulatedElapsedTime = TimeSpan.Zero;
            accumulatedFrameCountPerSecond = 0;
            FrameCount = 0;
        }

        public void ResetTimeFactor()
        {
            factor = 1;
        }

        #endregion
    }
}
