// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Stride.Core.IO;

public static class StreamExtensions
{
    [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
    private static unsafe T Read<T>(this Stream @this) where T : unmanaged
    {
        Unsafe.SkipInit(out T result);
        var span = new Span<byte>(&result, Unsafe.SizeOf<T>());
        var read = @this.Read(span);
        if (read != Unsafe.SizeOf<T>())
            throw new EndOfStreamException();
        return result;
    }
    [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
    private static unsafe void Write<T>(this Stream @this, T value) where T : unmanaged
    {
        var span = new Span<byte>(&value, Unsafe.SizeOf<T>());
        @this.Write(span);
    }
    /// <summary>A temporary replacement for <see cref="NativeStream.ReadUInt16"/></summary>
    [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
    public static ushort ReadUInt16(this Stream @this) => @this.Read<ushort>();
    /// <summary>A temporary replacement for <see cref="NativeStream.ReadUInt32"/></summary>
    [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
    public static uint   ReadUInt32(this Stream @this) => @this.Read<uint  >();
    /// <summary>A temporary replacement for <see cref="NativeStream.ReadUInt64"/></summary>
    [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
    public static ulong  ReadUInt64(this Stream @this) => @this.Read<ulong >();
    /// <summary>A temporary replacement for <see cref="NativeStream.Write(ushort)"/></summary>
    [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
    public static void Write(this Stream @this, ushort value) => @this.Write<ushort>(value);
    /// <summary>A temporary replacement for <see cref="NativeStream.Write(uint)"/></summary>
    [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
    public static void Write(this Stream @this, uint   value) => @this.Write<uint  >(value);
    /// <summary>A temporary replacement for <see cref="NativeStream.Write(ulong)"/></summary>
    [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
    public static void Write(this Stream @this, ulong  value) => @this.Write<ulong >(value);
}
