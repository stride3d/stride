// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D
// Copyright (c) 2010-2011 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using Xenko.Core.Mathematics;

namespace Xenko.Graphics
{
    internal class ColorHelper
    {
        public static unsafe RawColor4 Convert(Color4 color)
        {
            return *(RawColor4*)&color;
        }

        public static unsafe Color4 Convert(RawColor4 color)
        {
            return *(Color4*)&color;
        }

        public static unsafe RawVector4 ConvertToVector4(Color4 color)
        {
            return *(RawVector4*)&color;
        }

#if XENKO_GRAPHICS_API_DIRECT3D12
        public static unsafe SharpDX.Direct3D12.StaticBorderColor ConvertStatic(Color4 color)
        {
            if (color == Color4.Black)
            {
                return SharpDX.Direct3D12.StaticBorderColor.OpaqueBlack;
            }
            else if (color == Color4.White)
            {
                return SharpDX.Direct3D12.StaticBorderColor.OpaqueWhite;
            }
            else if (color == new Color4())
            {
                return SharpDX.Direct3D12.StaticBorderColor.TransparentBlack;
            }

            throw new NotSupportedException("Static sampler can only have opaque black, opaque white or transparent white as border color.");
        }
#endif
    }
}

 
#endif 
