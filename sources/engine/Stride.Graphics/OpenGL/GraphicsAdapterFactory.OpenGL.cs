// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL 
namespace Stride.Graphics
{
    public partial class GraphicsAdapterFactory
    {
        private static void InitializeInternal()
        {
            defaultAdapter = new GraphicsAdapter();
            adapters = new [] { defaultAdapter };
        }
    }
} 
#endif
