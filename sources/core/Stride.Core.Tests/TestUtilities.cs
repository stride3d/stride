// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Stride.Core.Tests
{
    public class TestUtilities
    {
        [StructLayout(LayoutKind.Sequential, Size = 8)]
        struct S
        {
            public int A;
            public int B;
        }

        [Fact]
        public unsafe void Base()
        {
            // Allocate memory
            var data = Utilities.AllocateMemory(32, 16);

            // Check allocation and alignment
            Assert.True(data != IntPtr.Zero);
            Assert.True(((long)data % 16) == 0);

            var s = new S { A = 32, B = 33 };

            // Check SizeOf
            Assert.Equal(8, Utilities.SizeOf<S>());

            // Write
            Utilities.Write(data + 4, ref s);
            var s2 = new S();
            Utilities.Read(data + 4, ref s2);
            Assert.Equal(s, s2);

            // CopyMemory+Fixed (with offset)
            Utilities.CopyMemory(data + 12, (IntPtr)Interop.Fixed(ref s) + 4, Utilities.SizeOf<int>());
            int b = 0;
            Utilities.Read(data + 12, ref b);
            Assert.Equal(s.B, b);
            
            // FreeMemory
            Utilities.FreeMemory(data);
        }
    }
}
