// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using System.Diagnostics;
using Xenko.Core;

namespace Xenko.Games.Time
{
    /// <summary>
    /// This provides timing information similar to <see cref="System.Diagnostics.Stopwatch"/> but an update occurring only on a <see cref="Tick"/> method.
    /// </summary>
    public class TimerTick
    {
        #region Fields

        private long startRawTime;
        private long lastRawTime;
        private int pauseCount;
        private long pauseStartTime;
        private long timePaused;
        private decimal speedFactor;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerTick"/> class.
        /// </summary>
        public TimerTick()
        {
            speedFactor = 1.0m;
            Reset();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerTick" /> class.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        public TimerTick(TimeSpan startTime)
        {
            speedFactor = 1.0m;
            Reset(startTime);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the start time when this timer was created.
        /// </summary>
        public TimeSpan StartTime { get; private set; }

        /// <summary>
        /// Gets the total time elasped since the last reset or when this timer was created.
        /// </summary>
        public TimeSpan TotalTime { get; private set; }

        /// <summary>
        /// Gets the total time elasped since the last reset or when this timer was created, including <see cref="Pause"/>
        /// </summary>
        public TimeSpan TotalTimeWithPause { get; private set; }

        /// <summary>
        /// Gets the elapsed time since the previous call to <see cref="Tick"/>.
        /// </summary>
        public TimeSpan ElapsedTime { get; private set; }

        /// <summary>
        /// Gets the elapsed time since the previous call to <see cref="Tick"/> including <see cref="Pause"/>
        /// </summary>
        public TimeSpan ElapsedTimeWithPause { get; private set; }

        /// <summary>
        /// Gets or sets the speed factor. Default is 1.0
        /// </summary>
        /// <value>The speed factor.</value>
        public double SpeedFactor
        {
            get
            {
                return (double)speedFactor;
            }
            set
            {
                speedFactor = (decimal)value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is paused.
        /// </summary>
        /// <value><c>true</c> if this instance is paused; otherwise, <c>false</c>.</value>
        public bool IsPaused
        {
            get
            {
                return pauseCount > 0;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Resets this instance. <see cref="TotalTime"/> is set to zero.
        /// </summary>
        public void Reset()
        {
            Reset(TimeSpan.Zero);
        }

        /// <summary>
        /// Resets this instance. <see cref="TotalTime" /> is set to startTime.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        public void Reset(TimeSpan startTime)
        {
            StartTime = startTime;
            TotalTime = startTime;
            startRawTime = Stopwatch.GetTimestamp();
            lastRawTime = startRawTime;
            timePaused = 0;
            pauseStartTime = 0;
            pauseCount = 0;
        }

        /// <summary>
        /// Resumes this instance, only if a call to <see cref="Pause"/> has been already issued.
        /// </summary>
        public void Resume()
        {
            pauseCount--;
            if (pauseCount <= 0)
            {
                timePaused += Stopwatch.GetTimestamp() - pauseStartTime;
                pauseStartTime = 0L;
            }
        }

        /// <summary>
        /// Update the <see cref="TotalTime"/> and <see cref="ElapsedTime"/>,
        /// </summary>
        /// <remarks>
        /// This method must be called on a regular basis at every *tick*.
        /// </remarks>
        public void Tick()
        {
            // Don't tick when this instance is paused.
            if (IsPaused)
            {
                ElapsedTime = TimeSpan.Zero;
                return;
            }

            var rawTime = Stopwatch.GetTimestamp();
            TotalTime = StartTime + new TimeSpan((long)Math.Round(Utilities.ConvertRawToTimestamp(rawTime - timePaused - startRawTime).Ticks * speedFactor));
            TotalTimeWithPause = StartTime + new TimeSpan((long)Math.Round(Utilities.ConvertRawToTimestamp(rawTime - startRawTime).Ticks * speedFactor));

            ElapsedTime = Utilities.ConvertRawToTimestamp(rawTime - timePaused - lastRawTime);
            ElapsedTimeWithPause = Utilities.ConvertRawToTimestamp(rawTime - lastRawTime);

            if (ElapsedTime < TimeSpan.Zero)
            {
                ElapsedTime = TimeSpan.Zero;
            }

            lastRawTime = rawTime;
        }

        /// <summary>
        /// Pauses this instance.
        /// </summary>
        public void Pause()
        {
            pauseCount++;
            if (pauseCount == 1)
            {
                pauseStartTime = Stopwatch.GetTimestamp();
            }
        }

        #endregion
    }
}
