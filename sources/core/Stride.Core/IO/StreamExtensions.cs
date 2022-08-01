using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Stride.Core.IO;

internal static class StreamExtensions
{
    public static unsafe T Read<T>(this Stream @this) where T : unmanaged
    {
        Unsafe.SkipInit(out T result);
        var span = new Span<byte>(&result, Unsafe.SizeOf<T>());
        var read = @this.Read(span);
        if (read != Unsafe.SizeOf<T>())
            throw new EndOfStreamException();
        return result;
    }
    public static unsafe void Write<T>(this Stream @this, T value) where T : unmanaged
    {
        var span = new Span<byte>(&value, Unsafe.SizeOf<T>());
        @this.Write(span);
    }
    public static ushort ReadUInt16(this Stream @this) => @this.Read<ushort>();
    public static uint   ReadUInt32(this Stream @this) => @this.Read<uint  >();
    public static ulong  ReadUInt64(this Stream @this) => @this.Read<ulong >();
    public static void Write(this Stream @this, ushort value) => @this.Write<ushort>(value);
    public static void Write(this Stream @this, uint   value) => @this.Write<uint  >(value);
    public static void Write(this Stream @this, ulong  value) => @this.Write<ulong >(value);
}
