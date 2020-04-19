// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace Stride.Core.Presentation.Core
{
    /// <summary>
    /// A timer that will raise a <see cref="Timeout"/> event if it's not reset within a given <see cref="Delay"/>.
    /// </summary>
    public class TimeoutDispatcherTimer
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private DispatcherTimer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutDispatcherTimer"/> class.
        /// </summary>
        /// <param name="delay">The delay before the timer times out.</param>
        public TimeoutDispatcherTimer(int delay)
        {
            Delay = delay;
        }

        /// <summary>
        /// Gets or sets the delay before the timer times out.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// Raised when the timer times out.
        /// </summary>
        public event EventHandler<EventArgs> Timeout;

        /// <summary>
        /// Resets the timer, preventing to raise the <see cref="Timeout"/>.
        /// </summary>
        public void Reset()
        {
            stopwatch.Restart();
            if (timer == null)
            {
                timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = TimeSpan.FromMilliseconds(Delay) };
                timer.Tick += (s, args) => { if (stopwatch.ElapsedMilliseconds >= Delay) { timer.Stop(); timer = null; RaiseEvent(); } };
                timer.Start();
            }
            else
            {
                timer.Stop();
                timer.Start();
            }
        }

        private void RaiseEvent()
        {
            Timeout?.Invoke(this, EventArgs.Empty);
        }
    }
}
