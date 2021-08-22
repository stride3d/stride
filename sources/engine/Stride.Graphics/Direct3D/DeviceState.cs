using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Graphics.Direct3D
{
    public enum DeviceState
    {
        DeviceHung = -2005530508,

        DeviceLost = -2005530520,

        DeviceRemoved = -2005530512,

        Ok = 0,

        OutOfVideoMemory = -2005532292,

        PresentModeChanged = 0x8760877,

        PresentOccluded = 0x8760878
    }
}
