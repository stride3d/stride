using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Graphics.Direct3D
{
    public enum DXGISharedResourceAccess : ulong
    {
        DXGI_SHARED_RESOURCE_READ = 0x80000000UL,
        DXGI_SHARED_RESOURCE_WRITE = 1UL
    }
}
