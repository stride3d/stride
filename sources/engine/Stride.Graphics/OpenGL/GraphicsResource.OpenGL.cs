// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_OPENGL 

#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
using PixelFormatGl = OpenTK.Graphics.ES30.PixelFormat;
using PixelInternalFormat = OpenTK.Graphics.ES30.TextureComponentCount;
#else
using OpenTK.Graphics.OpenGL;
using PixelFormatGl = OpenTK.Graphics.OpenGL.PixelFormat;
#endif

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public partial class GraphicsResource
    {
        internal bool DiscardNextMap; // Used to internally force a WriteDiscard (to force a rename) with the GraphicsResourceAllocator

        // Shaader resource view (Texture or Texture Buffer)
        internal int TextureId;
        internal TextureTarget TextureTarget;
        internal PixelInternalFormat TextureInternalFormat;
        internal PixelFormatGl TextureFormat;
        internal PixelType TextureType;
        internal int TexturePixelSize;
    }
}
 
#endif
