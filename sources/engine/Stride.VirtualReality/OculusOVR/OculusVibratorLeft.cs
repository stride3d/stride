using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.VirtualReality.OculusOVR
{
    internal class OculusVibratorLeft : OculusVibrator
    {
        protected readonly IntPtr ovrSession;

        public OculusVibratorLeft(IntPtr ovrSession)
        {
            this.ovrSession = ovrSession;
        }
        protected override void SetOvrVibration(bool vibrationEnabled)
        {
            if (vibrationEnabled)
                OculusOvr.SetLeftVibration(ovrSession, 1, 1);
            else
                OculusOvr.SetLeftVibration(ovrSession, 0, 0);
        }
    }
}
