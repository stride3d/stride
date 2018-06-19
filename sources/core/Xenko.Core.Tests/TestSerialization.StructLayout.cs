// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Xenko.Core.Serialization;

namespace Xenko.Core.Tests
{
    public partial class TestSerialization
    {
        [DataContract]
        [StructLayout(LayoutKind.Auto)]
        public class StructLayoutAuto
        {
            public int C;
            public int B;
            public int A;
        }

        [DataContract]
        [StructLayout(LayoutKind.Explicit)]
        public class StructLayoutExplicit
        {
            [FieldOffset(0)]
            public int C;
            [FieldOffset(8)]
            public int B;
            [FieldOffset(4)]
            public int A;
        }

        [DataContract]
        [StructLayout(LayoutKind.Sequential)]
        public class StructLayoutSequential
        {
            public int C;
            public int B;
            public int A;
        }

        [TestCase(TestSerialization.SerializationBackend.Binary)]
        [Description("Test struct layouting")]
        public void TestStructLayout(TestSerialization.SerializationBackend serializationBackend)
        {
            var binaryAuto = SerializeBinary(new StructLayoutAuto { C = 1, B = 2, A = 3 });
            var binaryExplicit = SerializeBinary(new StructLayoutExplicit { C = 1, A = 2, B = 3 });
            var binarySequential = SerializeBinary(new StructLayoutSequential { C = 1, B = 2, A = 3 });

            var binaryExpected = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, binaryExpected, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(2), 0, binaryExpected, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(3), 0, binaryExpected, 8, 4);

            Assert.That(binaryAuto, Is.EqualTo(binaryExpected));
            Assert.That(binaryExplicit, Is.EqualTo(binaryExpected));
            Assert.That(binarySequential, Is.EqualTo(binaryExpected));
        }
    }
}
