// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics.Semantic;

namespace Stride.Graphics;

/// <example>
/// Reading the vertex positions of a mesh:
/// <code>
/// <![CDATA[
/// Model.Meshes[0].Draw.VertexBuffers[0].AsReadable(Services, out VertexBufferHelper helper, out int count);
/// var vertexPositions = new Vector3[count];
/// helper.Copy<PositionSemantic, Vector3>(vertexPositions);
/// ]]>
/// </code>
/// </example>
public class VertexBufferHelper
{
    /// <summary>
    /// Full vertex buffer, does not account for the binding offset or length
    /// </summary>
    public readonly byte[] DataOuter;
    public readonly VertexBufferBinding Binding;

    /// <summary>
    /// Effective vertex buffer, accounts for the binding offset and length
    /// </summary>
    public Span<byte> DataInner => DataOuter.AsSpan(Binding.Offset, Binding.Count * Binding.Stride);

    /// <inheritdoc cref="MeshExtension.AsReadable(VertexBufferBinding, IServiceRegistry, out VertexBufferHelper, out int)"/>
    public VertexBufferHelper(VertexBufferBinding binding, IServiceRegistry services, out int count)
    {
        var data = MeshExtension.FetchBufferContentOrThrow(binding.Buffer, services);
        DataOuter = data;
        Binding = binding;
        count = Binding.Count;
    }

    /// <summary>
    /// Extract individual element from each vertex contained in this vertex buffer and copies them into <paramref name="buffer"/>
    /// </summary>
    /// <param name="buffer">
    /// The buffer which will be written to, must have exactly the same amount of items as there are <b>vertices</b> in the buffer
    /// </param>
    /// <param name="semanticIndex">
    /// The semantic to read with that index, starts at zero.<br/>
    /// For example, to sample the second TextureCoordinate, you would use
    /// <code>
    /// <![CDATA[
    /// helper.Copy<TextureCoordinateSemantic, Vector2>(myUvs, 1);
    /// ]]>
    /// </code>
    /// </param>
    /// <typeparam name="TSemantic">The semantic to read, for example <see cref="PositionSemantic"/></typeparam>
    /// <typeparam name="TValue">The value type to read, depends entirely on the <typeparamref name="TSemantic"/> used</typeparam>
    /// <returns>True when this semantic exists in the vertex buffer, false otherwise</returns>
    /// <exception cref="NotImplementedException">
    /// When the data format for this semantic is too arcane - no conversion logic is implemented for that type
    /// </exception>
    /// <inheritdoc cref="VertexBufferHelper"/>
    public bool Copy<TSemantic, TValue>(Span<TValue> buffer, int semanticIndex = 0) where TSemantic : ISemantic<TValue> where TValue : unmanaged
    {
        return Read<TSemantic, TValue, CopyToDest<TValue>>(buffer, new CopyToDest<TValue>(), semanticIndex);
    }

    /// <summary>
    /// Provides custom access to the vertex buffer while having access to the auto-conversion
    /// </summary>
    /// <param name="destination">
    /// The destination span your <typeparamref name="TReader"/> <see cref="IReader{TDest}.Read{TConversion, TValue}"/> method receives,
    /// you may pass <see cref="Span{T}.Empty"/> if you do not need one.
    /// </param>
    /// <param name="reader">
    /// An implementation of <see cref="IReader{TDestValue}"/>, implement this interface to read directly from the vertex buffer
    /// while making use of the auto-conversion of the <typeparamref name="TSemantic"/> provided <br/>
    /// Preferably as a struct to ensure it is inlined by the JIT
    /// </param>
    /// <param name="semanticIndex">
    /// The semantic to read with that index, starts at zero.<br/>
    /// For example, to sample the second TextureCoordinate, you would use
    /// <code>
    /// <![CDATA[
    /// helper.Read<TextureCoordinateSemantic, Vector2>(yourReader, 1);
    /// ]]>
    /// </code>
    /// </param>
    /// <typeparam name="TSemantic">The semantic to read, <see cref="PositionSemantic"/> for example</typeparam>
    /// <typeparam name="TDest">The value type to read, depends on the <typeparamref name="TSemantic"/> used, <see cref="Vector3"/> when your <typeparamref name="TSemantic"/> <see cref="PositionSemantic"/> for example</typeparam>
    /// <typeparam name="TReader">The type of the reader you're providing</typeparam>
    /// <returns>True when this semantic exists in the vertex buffer, false otherwise</returns>
    /// <exception cref="NotImplementedException">
    /// When the data format for this semantic is too arcane - no conversion logic is implemented for that type
    /// </exception>
    /// <inheritdoc cref="IReader{TDest}"/>
    public bool Read<TSemantic, TDest, TReader>(Span<TDest> destination, TReader reader, int semanticIndex = 0)
        where TSemantic : ISemantic<TDest> where TDest : unmanaged
        where TReader : IReader<TDest>
    {
        if (Binding.TryGetElement(TSemantic.Name, semanticIndex, out var elementData))
        {
            switch (elementData.VertexElement.Format)
            {
                case PixelFormat.R32G32_Float: Inner<TSemantic, TReader, Vector2, TDest>(destination, reader, elementData); break;
                case PixelFormat.R32G32B32_Float: Inner<TSemantic, TReader, Vector3, TDest>(destination, reader, elementData); break;
                case PixelFormat.R32G32B32A32_Float: Inner<TSemantic, TReader, Vector4, TDest>(destination, reader, elementData); break;
                case PixelFormat.R16G16_Float: Inner<TSemantic, TReader, Half2, TDest>(destination, reader, elementData); break;
                case PixelFormat.R16G16B16A16_Float: Inner<TSemantic, TReader, Half4, TDest>(destination, reader, elementData); break;
                case PixelFormat.R16G16B16A16_UInt: Inner<TSemantic, TReader, UShort4, TDest>(destination, reader, elementData); break;
                case PixelFormat.R8G8B8A8_UInt: Inner<TSemantic, TReader, Byte4, TDest>(destination, reader, elementData); break;
                default: throw new NotImplementedException($"Unsupported format when converting vertex element ({elementData.VertexElement.Format})");
            }

            return true;
        }

        return false;
    }

    private unsafe void Inner<TConversion, TReader, TSource, TDest>(Span<TDest> destination, TReader reader, VertexElementWithOffset element) 
        where TConversion : IConversion<TSource, TDest> 
        where TSource : unmanaged
        where TReader : IReader<TDest>
    {
        if (sizeof(TSource) != element.Size)
            throw new ArgumentException($"{typeof(TSource)} does not match element size ({sizeof(TSource)} != {element.Size})");

        var stride = Binding.Declaration.VertexStride;
        var offset = element.Offset;
        var count = Binding.Count;
            
        fixed (byte* ptrSr = DataInner)
        {
            byte* firstElement = ptrSr + offset;
            reader.Read<TConversion, TSource>(firstElement, count, stride, destination);
        }
    }

    public struct CopyAsTriangleList : IReader<Vector3>
    {
        public required IndexBufferHelper IndexBufferHelper;
        
        public unsafe void Read<TConversion, TSource>(byte* sourcePointer, int elementCount, int stride, Span<Vector3> destination)
            where TConversion : IConversion<TSource, Vector3> where TSource : unmanaged
        {
            if (destination.Length != IndexBufferHelper.Binding.Count)
                throw new ArgumentException($"{nameof(destination)} length does not match the amount of indices contained within the index buffer buffer ({destination.Length} / {IndexBufferHelper.Binding.Count})");

            fixed (Vector3* destPtr = destination)
            {
                Vector3* dest = destPtr;
                if (IndexBufferHelper.Is32Bit(out var indices32, out var indices16))
                {
                    foreach (var index in indices32)
                    {
                        TConversion.Convert(*(TSource*)(sourcePointer + index * stride), out *dest);
                        dest++;
                    }
                }
                else
                {
                    foreach (var index in indices16)
                    {
                        TConversion.Convert(*(TSource*)(sourcePointer + index * stride), out *dest);
                        dest++;
                    }
                }
            }
        }
    }

    private struct CopyToDest<T> : IReader<T> where T : unmanaged
    {
        public unsafe void Read<TConversion, TSource>(byte* sourcePointer, int elementCount, int stride, Span<T> destination) 
            where TConversion : IConversion<TSource, T> 
            where TSource : unmanaged
        {
            if (destination.Length != elementCount)
                throw new ArgumentException($"{nameof(destination)} length does not match the amount of vertices contained within this vertex buffer ({destination.Length} / {elementCount})");
            
            fixed (T* ptrDest = destination)
            {
                byte* end = sourcePointer + elementCount * stride;
                T* dest = ptrDest;
                for (; sourcePointer < end; sourcePointer += stride, dest++)
                    TConversion.Convert(*(TSource*)sourcePointer, out *dest);
            }
        }
    }

    /// <example>
    /// Implementing <see cref="Copy{TSemantic,TValue}"/> manually:
    /// <code>
    /// <![CDATA[
    /// Model.Meshes[0].Draw.VertexBuffers[0].AsReadable(Services, out VertexBufferHelper helper, out int count);
    /// var vertexPositions = new Vector3[count];
    /// var myReader = new CopyTo();
    /// helper.Read<PositionSemantic, Vector3, CopyTo>(vertexPositions, myReader);
    /// 
    /// struct CopyTo : IReader<Vector3>
    /// {
    ///    public unsafe void Read<TConversion, TSource>(byte* sourcePointer, int elementCount, int stride, Span<T> destination) 
    ///     where TConversion : IConversion<TSource, T> where TSource : unmanaged
    ///    {
    ///        if (destination.Length != elementCount)
    ///            throw new ArgumentException($"{nameof(destination)} length does not match the amount of vertices contained within this vertex buffer ({destination.Length} / {elementCount})");
    ///            
    ///        fixed (T* ptrDest = destination)
    ///        {
    ///            byte* end = sourcePointer + elementCount * stride;
    ///            T* dest = ptrDest;
    ///            for (; sourcePointer < end; sourcePointer += stride, dest++)
    ///                TConversion.Convert(*(TSource*)sourcePointer, out *dest);
    ///        }
    ///    }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public interface IReader<TDest>
    {
        /// <param name="sourcePointer">Points to the first element in the vertex buffer, read it as a TSource* to retrieve its value</param>
        /// <param name="elementCount">The amount of vertices. This is not equivalent to the size of the vertex buffer, or the size in bytes taken by individual vertices</param>
        /// <param name="stride">The size in bytes taken by individual vertices, add it to <paramref name="sourcePointer"/> to point to the next element</param>
        /// <param name="destination">The span passed into the <see cref="Read{TConversion,TSource}"/> method call</param>
        /// <typeparam name="TConversion">A helper to convert between <typeparamref name="TSource"/> and <typeparamref name="TDest"/> properly</typeparam>
        /// <typeparam name="TSource">
        /// The source type this vertex buffer was built with, for example <see cref="Vector2"/> or <see cref="Byte4"/>,
        /// use <typeparamref name="TConversion"/> to convert it into a <typeparamref name="TDest"/>.
        /// </typeparam>
        /// <inheritdoc cref="IReader{TDest}"/>
        unsafe void Read<TConversion, TSource>(byte* sourcePointer, int elementCount, int stride, Span<TDest> destination)
            where TConversion : IConversion<TSource, TDest> where TSource : unmanaged;
    }
}
