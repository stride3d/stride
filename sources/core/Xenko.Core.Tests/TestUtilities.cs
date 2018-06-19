// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Xenko.Core.Tests
{
    [TestFixture]
    public class TestUtilities
    {
        [StructLayout(LayoutKind.Sequential, Size = 8)]
        struct S
        {
            public int A;
            public int B;
        }

        [Test]
        public unsafe void Base()
        {
            // Allocate memory
            var data = Utilities.AllocateMemory(32, 16);

            // Check allocation and alignment
            Assert.That(data != IntPtr.Zero);
            Assert.That(((long)data % 16) == 0);

            var s = new S { A = 32, B = 33 };

            // Check SizeOf
            Assert.That(Utilities.SizeOf<S>(), Is.EqualTo(8));

            // Write
            Utilities.Write(data + 4, ref s);
            var s2 = new S();
            Utilities.Read(data + 4, ref s2);
            Assert.That(s2, Is.EqualTo(s));

            // CopyMemory+Fixed (with offset)
            Utilities.CopyMemory(data + 12, (IntPtr)Interop.Fixed(ref s) + 4, Utilities.SizeOf<int>());
            int b = 0;
            Utilities.Read(data + 12, ref b);
            Assert.That(b, Is.EqualTo(s.B));
            
            // FreeMemory
            Utilities.FreeMemory(data);
        }
    }
}
