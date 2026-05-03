// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace Stride.Core.Tests;

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
        var data = MemoryUtilities.Allocate(sizeInBytes: 32, alignment: 16);

        // Check allocation and alignment
        Assert.True(data != IntPtr.Zero);
        Assert.Equal(0, (long)data % 16);

        // FreeMemory
        MemoryUtilities.Free(data);
    }
}
