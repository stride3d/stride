// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Stride.TextureConverter.Tests
{
    public class TestToolsTests
    {
        [Fact]
        public void ComputeSHA1MatchesForFileAndMemory()
        {
            var bytes = Encoding.ASCII.GetBytes("abc");
            var path = Path.GetTempFileName();
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                File.WriteAllBytes(path, bytes);
                const string expected = "a9993e364706816aba3e25717850c26c9cd0d89d";
                Assert.Equal(expected, TestTools.ComputeSHA1(path));
                Assert.Equal(expected, TestTools.ComputeSHA1(handle.AddrOfPinnedObject(), bytes.Length));
            }
            finally
            {
                handle.Free();
                File.Delete(path);
            }
        }
    }
}
