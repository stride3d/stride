using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A disposable buffer wrapper using the HighPerformance toolkit.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BufferBase<T>
    where T : struct
{
    internal MemoryOwner<T> _owner = MemoryOwner<T>.Empty;
    /// <summary>
    /// Span accessor of the data represented by the buffer
    /// </summary>
    public virtual Span<T> Span => _owner.Span[..Length];
    /// <summary>
    /// Memory accessor of the data represented by the buffer
    /// </summary>
    public virtual Memory<T> Memory => _owner.Memory[..Length];
    /// <summary>
    /// Corresponding bytes
    /// </summary>
    public Span<byte> Bytes => MemoryMarshal.AsBytes(Span);
    /// <summary>
    /// Length of the buffer
    /// </summary>
    public int Length { get; protected set; }
    public void Dispose() => _owner.Dispose();
}