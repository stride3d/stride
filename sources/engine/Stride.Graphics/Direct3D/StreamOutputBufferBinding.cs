using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Direct3D11;

namespace Stride.Graphics.Direct3D
{
    public struct StreamOutputBufferBinding
    {
        public ID3D11Buffer Buffer;
        public uint Offset;
    }
}
