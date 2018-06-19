// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Graphics
{
    public struct BufferPoolAllocationResult
    {
        public IntPtr Data;
        public int Size;
        public int Offset;

        public bool Uploaded;
        public Buffer Buffer;
    }
}
