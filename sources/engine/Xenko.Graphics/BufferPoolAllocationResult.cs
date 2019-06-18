// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;

namespace Xenko.Graphics
{
    public struct BufferPoolAllocationResult
    {
        private int uploaded;
        
        public IntPtr Data;
        public int Size;
        public int Offset;
        public Buffer Buffer;

        public bool Uploaded
        {
            get { return uploaded == 0; }
            set { uploaded = value ? 1 : 0; }
        }

        public bool TrySetUploaded()
        {
            return Interlocked.CompareExchange(ref uploaded, 1, 0) == 0;
        }
    }
}
