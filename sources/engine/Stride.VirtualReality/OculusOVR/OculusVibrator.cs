using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenXR;

namespace Stride.VirtualReality.OculusOVR
{
    internal abstract class OculusVibrator
    {
        protected int concurrentCallCount;
        /// <summary>
        /// Vibrate for a number of milliseconds
        /// </summary>
        /// <param name="duration">The number of milliseconds to vibrate for</param>
        /// <returns></returns>
        public async Task Vibrate(int duration)
        {
            concurrentCallCount++;
            while(duration > 2000)
            {
                SetOvrVibration(true);
                duration -= 2000;
                await Task.Delay(2000);
            }
            SetOvrVibration(true);
            await Task.Delay(duration);
            concurrentCallCount--;
            if(concurrentCallCount == 0)
                SetOvrVibration(false);
        }
        /// <summary>
        /// Enable or disable vibration. Should be called periodically, vibration automatically disables after 2.5 seconds.
        /// </summary>
        /// <param name="vibrationEnabled">true to start vibration, false to stop vibration</param>
        protected abstract void SetOvrVibration(bool vibrationEnabled);
    }
}
