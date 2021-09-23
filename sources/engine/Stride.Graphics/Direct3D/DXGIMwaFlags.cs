using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Graphics.Direct3D
{
    public enum DXGIMwaFlags : uint
    {
        NO_WINDOW_CHANGES = 0x1,
        NO_ALT_ENTER = 0x2,
        NO_PRINT_SCREEN = 0x4,
        VALID = 0x7
    }
}
