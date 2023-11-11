using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.VirtualReality.OculusOVR
{
    internal class OculusVibratorRight : OculusVibrator
    {
        protected readonly IntPtr ovrSession;
        public OculusVibratorRight(IntPtr ovrSession)
        {
            this.ovrSession = ovrSession;
        }
        protected override void SetOvrVibration(bool vibrationEnabled)
        {
            if (vibrationEnabled)
                OculusOvr.SetRightVibration(ovrSession, 1, 1);
            else
                OculusOvr.SetRightVibration(ovrSession, 0, 0);
        }
    }
}
