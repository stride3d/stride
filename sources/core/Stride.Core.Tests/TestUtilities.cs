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

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public unsafe void AllocateMemory_WithValidAlignment_AllocatesAlignedMemory(int alignment)
    {
        var data = Utilities.AllocateMemory(128, alignment);

        Assert.True(data != IntPtr.Zero);
        Assert.Equal(0, (long)data % alignment);

        Utilities.FreeMemory(data);
    }

    [Fact]
    public unsafe void AllocateMemory_WithInvalidAlignment_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Utilities.AllocateMemory(32, 15));
    }

    [Fact]
    public unsafe void AllocateClearedMemory_InitializesMemoryToZero()
    {
        var size = 128;
        var data = Utilities.AllocateClearedMemory(size);

        var span = new Span<byte>((void*)data, size);
        foreach (var b in span)
        {
            Assert.Equal(0, b);
        }

        Utilities.FreeMemory(data);
    }

    [Fact]
    public unsafe void AllocateClearedMemory_WithCustomClearValue_InitializesMemoryToValue()
    {
        var size = 128;
        var clearValue = (byte)0xFF;
        var data = Utilities.AllocateClearedMemory(size, clearValue);

        var span = new Span<byte>((void*)data, size);
        foreach (var b in span)
        {
            Assert.Equal(clearValue, b);
        }

        Utilities.FreeMemory(data);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void IsMemoryAligned_WithAlignedPointer_ReturnsTrue(int alignment)
    {
        unsafe
        {
            var data = Utilities.AllocateMemory(128, alignment);

            Assert.True(Utilities.IsMemoryAligned(data, alignment));

            Utilities.FreeMemory(data);
        }
    }

    [Fact]
    public void IsMemoryAligned_WithInvalidAlignment_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Utilities.IsMemoryAligned(IntPtr.Zero, 15));
    }

    [Fact]
    public void Dispose_WithNullObject_DoesNothing()
    {
        IDisposable? disposable = null;
        Utilities.Dispose(ref disposable);
        Assert.Null(disposable);
    }

    [Fact]
    public void Dispose_WithDisposableObject_DisposesAndSetsToNull()
    {
        var disposed = false;
        IDisposable? disposable = new DisposableTestClass(() => disposed = true);

        Utilities.Dispose(ref disposable);

        Assert.True(disposed);
        Assert.Null(disposable);
    }

    [Fact]
    public void GetHashCode_WithDictionary_ReturnsConsistentHashCode()
    {
        var dict = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2
        };

        var hash1 = Utilities.GetHashCode(dict);
        var hash2 = Utilities.GetHashCode(dict);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_WithNullDictionary_ReturnsZero()
    {
        var hash = Utilities.GetHashCode((System.Collections.IDictionary)null!);
        Assert.Equal(0, hash);
    }

    [Fact]
    public void GetHashCode_WithEnumerable_ReturnsConsistentHashCode()
    {
        var list = new List<int> { 1, 2, 3 };

        var hash1 = Utilities.GetHashCode((System.Collections.IEnumerable)list);
        var hash2 = Utilities.GetHashCode((System.Collections.IEnumerable)list);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Compare_WithIdenticalDictionaries_ReturnsTrue()
    {
        var dict1 = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var dict2 = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        Assert.True(Utilities.Compare(dict1, dict2));
    }

    [Fact]
    public void Compare_WithDifferentDictionaries_ReturnsFalse()
    {
        var dict1 = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var dict2 = new Dictionary<string, int> { ["a"] = 1, ["c"] = 3 };

        Assert.False(Utilities.Compare(dict1, dict2));
    }

    [Fact]
    public void Compare_WithSameReference_ReturnsTrue()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };
        Assert.True(Utilities.Compare(dict, dict));
    }

    [Fact]
    public void Swap_ExchangesValues()
    {
        var a = 5;
        var b = 10;

        Utilities.Swap(ref a, ref b);

        Assert.Equal(10, a);
        Assert.Equal(5, b);
    }

    private class DisposableTestClass : IDisposable
    {
        private readonly Action onDispose;

        public DisposableTestClass(Action onDispose)
        {
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            onDispose();
        }
    }
}
