// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_OPENGL 

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public partial class GraphicsResource
    {
        internal bool DiscardNextMap; // Used to internally force a WriteDiscard (to force a rename) with the GraphicsResourceAllocator

        // Shaader resource view (Texture or Texture Buffer)
        internal uint TextureId;
        internal TextureTarget TextureTarget;
        internal InternalFormat TextureInternalFormat;
        internal PixelFormatGl TextureFormat;
        internal PixelType TextureType;
        internal int TexturePixelSize;
    }
}
 
#endif
