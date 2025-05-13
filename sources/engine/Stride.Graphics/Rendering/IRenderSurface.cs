using System;

namespace Stride.Graphics.Rendering;
public interface IRenderSurface
{
    public IntPtr Handle { get; protected set; }
    public int Width { get; protected set; }
    public int Height { get; protected set; }
}
