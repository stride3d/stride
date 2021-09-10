using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Direct3D11;

namespace Stride.Graphics.Direct3D.Extensions
{
    public static class BlendStateDescriptionExtensions
    {
        public static RenderTargetBlendDesc GetRenderTarget(this BlendDesc b, int i)
        {
            if (i == 0) return b.RenderTarget.Element0;
            else if (i == 1) return b.RenderTarget.Element1;
            else if (i == 2) return b.RenderTarget.Element2;
            else if (i == 3) return b.RenderTarget.Element3;
            else if (i == 4) return b.RenderTarget.Element4;
            else if (i == 5) return b.RenderTarget.Element5;
            else if (i == 6) return b.RenderTarget.Element6;
            else return b.RenderTarget.Element7;
        }
    }
}
