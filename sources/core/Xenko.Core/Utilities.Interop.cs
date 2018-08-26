// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
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
#pragma warning disable SA1300 // Element must begin with upper-case letter
#pragma warning disable SA1649 // File name must match first type name
using System;
using System.Runtime.CompilerServices;

namespace Xenko.Core
{
    /// <summary>
    /// Utility class.
    /// </summary>
    internal sealed class Interop
    {
        public static T Pin<T>(ref T source) where T : struct
        {
            throw new NotImplementedException();
        }

        public static T IncrementPinned<T>(T source) where T : struct
        {
            throw new NotImplementedException();
        }

        public static T AddPinned<T>(T source, int offset) where T : struct
        {
            throw new NotImplementedException();
        }

        public static void Pin<T>(T data) where T : class
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Fixed<T>(ref T data)
        {
            throw new NotImplementedException();
        }

        public static unsafe void* FixedOut<T>(out T data)
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Fixed<T>(T[] data)
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Cast<T>(ref T data) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe void* CastOut<T>(out T data) where T : struct
        {
            throw new NotImplementedException();
        }

        public static TCAST[] CastArray<TCAST, T>(T[] arrayData)
            where T : struct
            where TCAST : struct
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void memcpy(void* pDest, void* pSrc, int count)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void memset(void* pDest, byte value, int count)
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Read<T>(void* pSrc, ref T data) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe T ReadInline<T>(void* pSrc) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe void WriteInline<T>(void* pDest, ref T data) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe void CopyInline<T>(ref T data, void* pSrc) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe void CopyInline<T>(void* pDest, ref T srcData)
        {
            throw new NotImplementedException();
        }

        public static unsafe void CopyInlineOut<T>(out T data, void* pSrc)
        {
            throw new NotImplementedException();
        }

        public static unsafe void* ReadOut<T>(void* pSrc, out T data) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Read<T>(void* pSrc, T[] data, int offset, int count) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Read2D<T>(void* pSrc, T[,] data, int offset, int count) where T : struct
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Write<T>(void* pDest, ref T data) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Write<T>(void* pDest, T[] data, int offset, int count) where T : struct
        {
            throw new NotImplementedException();
        }

        public static unsafe void* Write2D<T>(void* pDest, T[,] data, int offset, int count) where T : struct
        {
            throw new NotImplementedException();
        }
    }
}
