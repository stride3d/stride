// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D11

namespace Xenko.Graphics
{
    public partial struct CompiledCommandList
    {
        internal SharpDX.Direct3D11.CommandList NativeCommandList;
    }
}

#endif