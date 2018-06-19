// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_NULL 
using System;

namespace Xenko.Graphics
{
    public partial class SamplerState
    {
        private SamplerState(GraphicsDevice graphicsDevice, SamplerStateDescription samplerStateDescription)
        {
            throw new NotImplementedException();
        }
    }
} 
#endif 
