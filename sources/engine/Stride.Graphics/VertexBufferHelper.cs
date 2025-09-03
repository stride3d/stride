// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics.Semantics;

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
public readonly struct VertexBufferHelper
{
    /// <summary>
    /// Full vertex buffer, the start and end of this buffer may contain data that does not map to this <see cref="Binding"/>,
    /// use <see cref="DataInner"/> if you want to only read the data that is mapped to this binding.
    /// </summary>
    public readonly byte[] DataOuter;
    public readonly VertexBufferBinding Binding;

    /// <summary>
    /// Effective vertex buffer, accounts for the binding offset and length
    /// </summary>
    public Span<byte> DataInner => DataOuter.AsSpan(Binding.Offset, Binding.Count * Binding.Stride);

    /// <inheritdoc cref="MeshExtension.AsReadable(VertexBufferBinding, IServiceRegistry, out VertexBufferHelper, out int)"/>
    public VertexBufferHelper(VertexBufferBinding binding, IServiceRegistry services, out int count) 
        : this(binding, MeshExtension.FetchBufferContentOrThrow(binding.Buffer, services), out count)
    {
    }

    /// <summary>
    /// Create the helper from existing data instead of trying to fetch the buffer automatically
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="dataOuter"/> does not match the binding definition provided,
    /// <paramref name="dataOuter"/> must be the entire vertex buffer
    /// </exception>
    public VertexBufferHelper(VertexBufferBinding binding, byte[] dataOuter, out int count)
    {
        if (dataOuter.Length < binding.Offset + binding.Count * binding.Stride)
            throw new ArgumentException($"{nameof(dataOuter)} does not fit the bindings provided. Make sure that the span provided contains the entirety of the vertex buffer");

        DataOuter = dataOuter;
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
    public bool Copy<TSemantic, TValue>(Span<TValue> buffer, int semanticIndex = 0) where TSemantic : 
        IConverter<Vector2, TValue>,
        IConverter<Vector3, TValue>,
        IConverter<Vector4, TValue>,
        IConverter<Half2, TValue>,
        IConverter<Half4, TValue>,
        IConverter<UShort4, TValue>,
        IConverter<Byte4, TValue>,
        ISemantic
        where TValue : unmanaged
    {
        return Read<TSemantic, TValue, CopyToDest<TValue>>(buffer, new CopyToDest<TValue>(), semanticIndex);
    }

    /// <summary>
    /// Copies this vertex buffer's data to the vertex-buffer-like span provided.
    /// Any semantic data present in <paramref name="destination"/> that is not present in this buffer is left untouched.
    /// </summary>
    /// <param name="destination">
    /// The buffer which will be written to, must be of the same length as the amount of vertices in this buffer
    /// </param>
    /// <returns>True if every semantic element of <typeparamref name="TDest"/> was written to from this buffers' data, false if at least one semantic was missing</returns>
    /// <exception cref="NotImplementedException">
    /// When the data format for this semantic is too arcane - no conversion logic is implemented for that type
    /// </exception>
    /// <example>
    /// Copying a mesh's vertex positions, colors and UVs:
    /// <code>
    /// <![CDATA[
    /// Model.Meshes[0].Draw.VertexBuffers[0].AsReadable(Services, out VertexBufferHelper helper, out int count);
    /// var vertexPositionsColorsAndUVs = new VertexPositionColorTexture[count];
    /// helper.Copy(vertexPositionsColorsAndUVs);
    /// ]]>
    /// </code>
    /// </example>
    public unsafe bool Copy<TDest>(Span<TDest> destination) where TDest : unmanaged, IVertex
    {
        bool missing = false;
        var parameters = new InterleavedParameters(DataInner, MemoryMarshal.Cast<TDest, byte>(destination), Binding.Stride, sizeof(TDest), Binding.Count);
        foreach (var destDef in default(TDest).GetLayout().EnumerateWithOffsets())
        {
            if (Binding.Declaration.TryGetElement(destDef.VertexElement.SemanticName, destDef.VertexElement.SemanticIndex, out var srcDef))
            {
                var srcOffset = srcDef.Offset;
                var destOffset = destDef.Offset;

                var srcFormat = srcDef.VertexElement.Format;
                switch (destDef.VertexElement.Format)
                {
                    // The particular semantic used doesn't matter too much here, we're just abusing the relaxed definition to fit any TDest
                    case PixelFormat.R32G32_Float: SelectSrcType<Relaxed<PositionSemantic>, Vector2>(parameters, srcOffset, destOffset, srcFormat); break;
                    case PixelFormat.R32G32B32_Float: SelectSrcType<Relaxed<PositionSemantic>, Vector3>(parameters, srcOffset, destOffset, srcFormat); break;
                    case PixelFormat.R32G32B32A32_Float: SelectSrcType<Relaxed<PositionSemantic>, Vector4>(parameters, srcOffset, destOffset, srcFormat); break;
                    case PixelFormat.R16G16_Float: SelectSrcType<Relaxed<PositionSemantic>, Half2>(parameters, srcOffset, destOffset, srcFormat); break;
                    case PixelFormat.R16G16B16A16_Float: SelectSrcType<Relaxed<PositionSemantic>, Half4>(parameters, srcOffset, destOffset, srcFormat); break;
                    case PixelFormat.R16G16B16A16_UInt: SelectSrcType<Relaxed<PositionSemantic>, UShort4>(parameters, srcOffset, destOffset, srcFormat); break;
                    case PixelFormat.R8G8B8A8_UInt: SelectSrcType<Relaxed<PositionSemantic>, Byte4>(parameters, srcOffset, destOffset, srcFormat); break;
                    default: throw new NotImplementedException($"Unsupported format when converting vertex element ({srcDef.VertexElement.Format})");
                }
            }
            else
            {
                missing = true;
            }
        }

        return missing;

        static void SelectSrcType<TSemantic, TOutput>(InterleavedParameters param, int srcElemOffset, int destElemOffset, PixelFormat format) 
            where TSemantic : 
            IConverter<Vector2, TOutput>,
            IConverter<Vector3, TOutput>,
            IConverter<Vector4, TOutput>,
            IConverter<Half2, TOutput>,
            IConverter<Half4, TOutput>,
            IConverter<UShort4, TOutput>,
            IConverter<Byte4, TOutput>,
            ISemantic
            where TOutput : unmanaged
        {
            switch (format)
            {
                case PixelFormat.R32G32_Float: InterleavedCopy<TSemantic, Vector2, TOutput>(param, srcElemOffset, destElemOffset); break;
                case PixelFormat.R32G32B32_Float: InterleavedCopy<TSemantic, Vector3, TOutput>(param, srcElemOffset, destElemOffset); break;
                case PixelFormat.R32G32B32A32_Float: InterleavedCopy<TSemantic, Vector4, TOutput>(param, srcElemOffset, destElemOffset); break;
                case PixelFormat.R16G16_Float: InterleavedCopy<TSemantic, Half2, TOutput>(param, srcElemOffset, destElemOffset); break;
                case PixelFormat.R16G16B16A16_Float: InterleavedCopy<TSemantic, Half4, TOutput>(param, srcElemOffset, destElemOffset); break;
                case PixelFormat.R16G16B16A16_UInt: InterleavedCopy<TSemantic, UShort4, TOutput>(param, srcElemOffset, destElemOffset); break;
                case PixelFormat.R8G8B8A8_UInt: InterleavedCopy<TSemantic, Byte4, TOutput>(param, srcElemOffset, destElemOffset); break;
                default: throw new NotImplementedException($"Unsupported format when converting vertex element ({format})");
            }
        }
        
        static void InterleavedCopy<TConverter, TSourceSemVal, TDestSemVal>(InterleavedParameters param, int srcElemOffset, int destElemOffset) 
            where TConverter : IConverter<TSourceSemVal, TDestSemVal> 
            where TSourceSemVal : unmanaged
            where TDestSemVal : unmanaged
        {
            fixed (byte* srcStart = param.Source)
            fixed (byte* destStart = param.Destination)
            {
                for (byte* 
                     src = srcStart + srcElemOffset,
                     dest = destStart + destElemOffset,
                     endSrc = src + param.VertexCount * param.DestStride;
                     
                     src < endSrc;
                     
                     src += param.SourceStride, dest += param.DestStride)
                {
                    TConverter.Convert(*(TSourceSemVal*)src, out *(TDestSemVal*)dest);
                }
            }
        }
    }

    /// <summary>
    /// Lower level access to read into the vertex buffer
    /// </summary>
    /// <param name="destination">
    /// The destination span your <typeparamref name="TReader"/> <see cref="IReader{TDest}.Read{TConverter, TValue}"/> method receives,
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
        where TSemantic :
        IConverter<Vector2, TDest>,
        IConverter<Vector3, TDest>,
        IConverter<Vector4, TDest>,
        IConverter<Half2, TDest>,
        IConverter<Half4, TDest>,
        IConverter<UShort4, TDest>,
        IConverter<Byte4, TDest>,
        ISemantic 
        where TDest : unmanaged
        where TReader : IReader<TDest>
    {
        if (Binding.Declaration.TryGetElement(TSemantic.Name, semanticIndex, out var elementData))
        {
            switch (elementData.VertexElement.Format)
            {
                case PixelFormat.R32G32_Float: InnerRead<TSemantic, TReader, Vector2, TDest>(destination, reader, elementData); break;
                case PixelFormat.R32G32B32_Float: InnerRead<TSemantic, TReader, Vector3, TDest>(destination, reader, elementData); break;
                case PixelFormat.R32G32B32A32_Float: InnerRead<TSemantic, TReader, Vector4, TDest>(destination, reader, elementData); break;
                case PixelFormat.R16G16_Float: InnerRead<TSemantic, TReader, Half2, TDest>(destination, reader, elementData); break;
                case PixelFormat.R16G16B16A16_Float: InnerRead<TSemantic, TReader, Half4, TDest>(destination, reader, elementData); break;
                case PixelFormat.R16G16B16A16_UInt: InnerRead<TSemantic, TReader, UShort4, TDest>(destination, reader, elementData); break;
                case PixelFormat.R8G8B8A8_UInt: InnerRead<TSemantic, TReader, Byte4, TDest>(destination, reader, elementData); break;
                default: throw new NotImplementedException($"Unsupported format when converting vertex element ({elementData.VertexElement.Format})");
            }

            return true;
        }

        return false;
    }

    private unsafe void InnerRead<TConverter, TReader, TSource, TDest>(Span<TDest> destination, TReader reader, VertexElementWithOffset element) 
        where TConverter : IConverter<TSource, TDest> 
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
            reader.Read<TConverter, TSource>(firstElement, count, stride, destination);
        }
    }

    /// <summary>
    /// Lower level access to write directly to the vertex buffer
    /// </summary>
    /// <param name="writer">
    /// An implementation of <see cref="IWriter{TDestValue}"/>, implement this interface to write directly into the vertex buffer
    /// while making use of the auto-conversion of the <typeparamref name="TSemantic"/> provided <br/>
    /// Preferably as a struct to ensure it is inlined by the JIT
    /// </param>
    /// <param name="semanticIndex">
    /// The semantic to read with that index, starts at zero.<br/>
    /// For example, to sample the second TextureCoordinate, you would use
    /// <code>
    /// <![CDATA[
    /// helper.Write<TextureCoordinateSemantic, Vector2, YourWriter>(yourWriter, 1);
    /// ]]>
    /// </code>
    /// </param>
    /// <typeparam name="TSemantic">The semantic to read, <see cref="PositionSemantic"/> for example</typeparam>
    /// <typeparam name="TDest"> The concrete type this writer will work with </typeparam>
    /// <typeparam name="TWriter">
    /// A struct implementing <see cref="IWriter{TDest}"/> which will be called in turn to write
    /// into this buffer when this method is called.
    /// </typeparam>
    /// <returns>True when this semantic exists in the vertex buffer, false otherwise</returns>
    /// <exception cref="NotImplementedException">
    /// When the data format for this semantic is too arcane - no conversion logic is implemented for that type
    /// </exception>
    /// <inheritdoc cref="IWriter{TDest}"/>
    public bool Write<TSemantic, TDest, TWriter>(TWriter writer, int semanticIndex = 0)
        where TSemantic :
        IConverter<TDest, Vector2>,
        IConverter<TDest, Vector3>,
        IConverter<TDest, Vector4>,
        IConverter<TDest, Half2>,
        IConverter<TDest, Half4>,
        IConverter<TDest, UShort4>,
        IConverter<TDest, Byte4>, 
        IConverter<Vector2, TDest>,
        IConverter<Vector3, TDest>,
        IConverter<Vector4, TDest>,
        IConverter<Half2, TDest>,
        IConverter<Half4, TDest>,
        IConverter<UShort4, TDest>,
        IConverter<Byte4, TDest>, 
        ISemantic
        where TDest : unmanaged
        where TWriter : IWriter<TDest>
    {
        if (Binding.Declaration.TryGetElement(TSemantic.Name, semanticIndex, out var elementData))
        {
            switch (elementData.VertexElement.Format)
            {
                case PixelFormat.R32G32_Float: InnerWrite<TSemantic, TWriter, Vector2, TDest>(writer, elementData); break;
                case PixelFormat.R32G32B32_Float: InnerWrite<TSemantic, TWriter, Vector3, TDest>(writer, elementData); break;
                case PixelFormat.R32G32B32A32_Float: InnerWrite<TSemantic, TWriter, Vector4, TDest>(writer, elementData); break;
                case PixelFormat.R16G16_Float: InnerWrite<TSemantic, TWriter, Half2, TDest>(writer, elementData); break;
                case PixelFormat.R16G16B16A16_Float: InnerWrite<TSemantic, TWriter, Half4, TDest>(writer, elementData); break;
                case PixelFormat.R16G16B16A16_UInt: InnerWrite<TSemantic, TWriter, UShort4, TDest>(writer, elementData); break;
                case PixelFormat.R8G8B8A8_UInt: InnerWrite<TSemantic, TWriter, Byte4, TDest>(writer, elementData); break;
                default: throw new NotImplementedException($"Unsupported format when converting vertex element ({elementData.VertexElement.Format})");
            }

            return true;
        }

        return false;
    }

    private unsafe void InnerWrite<TConverter, TWriter, TSource, TDest>(TWriter reader, VertexElementWithOffset element) 
        where TConverter : IConverter<TSource, TDest>, IConverter<TDest, TSource>
        where TSource : unmanaged
        where TWriter : IWriter<TDest>
    {
        if (sizeof(TSource) != element.Size)
            throw new ArgumentException($"{typeof(TSource)} does not match element size ({sizeof(TSource)} != {element.Size})");

        var stride = Binding.Declaration.VertexStride;
        var offset = element.Offset;
        var count = Binding.Count;
            
        fixed (byte* ptrSr = DataInner)
        {
            byte* firstElement = ptrSr + offset;
            reader.Write<TConverter, TSource>(firstElement, count, stride);
        }
    }

    public struct CopyAsTriangleList : IReader<Vector3>
    {
        public required IndexBufferHelper IndexBufferHelper;
        
        public unsafe void Read<TConverter, TSource>(byte* sourcePointer, int elementCount, int stride, Span<Vector3> destination)
            where TConverter : IConverter<TSource, Vector3> where TSource : unmanaged
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
                        TConverter.Convert(*(TSource*)(sourcePointer + index * stride), out *dest);
                        dest++;
                    }
                }
                else
                {
                    foreach (var index in indices16)
                    {
                        TConverter.Convert(*(TSource*)(sourcePointer + index * stride), out *dest);
                        dest++;
                    }
                }
            }
        }
    }

    private struct CopyToDest<T> : IReader<T> where T : unmanaged
    {
        public unsafe void Read<TConverter, TSource>(byte* sourcePointer, int elementCount, int stride, Span<T> destination) 
            where TConverter : IConverter<TSource, T> 
            where TSource : unmanaged
        {
            if (destination.Length != elementCount)
                throw new ArgumentException($"{nameof(destination)} length does not match the amount of vertices contained within this vertex buffer ({destination.Length} / {elementCount})");
            
            fixed (T* ptrDest = destination)
            {
                byte* end = sourcePointer + elementCount * stride;
                T* dest = ptrDest;
                for (; sourcePointer < end; sourcePointer += stride, dest++)
                    TConverter.Convert(*(TSource*)sourcePointer, out *dest);
            }
        }
    }

    private readonly ref struct InterleavedParameters
    {
        public readonly Span<byte> Source, Destination;
        public readonly int SourceStride, DestStride;
        public readonly int VertexCount;

        public InterleavedParameters(Span<byte> source, Span<byte> destination, int sourceStride, int destStride, int vertexCount)
        {
            if (destination.Length / DestStride != vertexCount)
                throw new ArgumentException($"The length and stride of {nameof(destination)} does not match the vertices required ({destination.Length / DestStride} / {vertexCount})");
            if (source.Length / SourceStride != vertexCount)
                throw new ArgumentException($"The length and stride of {nameof(source)} does not match the vertices required ({source.Length / SourceStride} / {vertexCount})");
            
            Source = source;
            Destination = destination;
            SourceStride = sourceStride;
            DestStride = destStride;
            VertexCount = vertexCount;
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
    ///    public unsafe void Read<TConverter, TSource>(byte* sourcePointer, int elementCount, int stride, Span<T> destination) 
    ///     where TConverter : IConverter<TSource, T> where TSource : unmanaged
    ///    {
    ///        if (destination.Length != elementCount)
    ///            throw new ArgumentException($"{nameof(destination)} length does not match the amount of vertices contained within this vertex buffer ({destination.Length} / {elementCount})");
    ///            
    ///        fixed (T* ptrDest = destination)
    ///        {
    ///            byte* end = sourcePointer + elementCount * stride;
    ///            T* dest = ptrDest;
    ///            for (; sourcePointer < end; sourcePointer += stride, dest++)
    ///                TConverter.Convert(*(TSource*)sourcePointer, out *dest);
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
        /// <param name="destination">The span passed into the <see cref="Read{TConverter,TSource}"/> method call</param>
        /// <typeparam name="TConverter">A helper to convert between <typeparamref name="TSource"/> and <typeparamref name="TDest"/> properly</typeparam>
        /// <typeparam name="TSource">
        /// The source type this vertex buffer was built with, for example <see cref="Vector2"/> or <see cref="Byte4"/>,
        /// use <typeparamref name="TConverter"/> to convert it into a <typeparamref name="TDest"/>.
        /// </typeparam>
        /// <inheritdoc cref="IReader{TDest}"/>
        unsafe void Read<TConverter, TSource>(byte* sourcePointer, int elementCount, int stride, Span<TDest> destination)
            where TConverter : IConverter<TSource, TDest> where TSource : unmanaged;
    }

    /// <example>
    /// Writing directly to mesh color:
    /// <code>
    /// <![CDATA[
    /// Model.Meshes[0].Draw.VertexBuffers[0].AsReadable(Services, out VertexBufferHelper helper, out int count);
    /// // Write to colors if that semantic already exist in the buffer, otherwise returns false
    /// helper.Write<ColorSemantic, Vector4, MultColor>(new MultColor(){ Color = Color.Gray });
    /// // Upload changes to the GPU
    /// Model.Meshes[0].Draw.VertexBuffers[0].Buffer.Recreate(helper.DataOuter);
    /// 
    /// private struct MultColor : VertexBufferHelper.IWriter<Vector4>
    /// {
    ///    public Color Color;
    ///
    ///    public unsafe void Write<TConverter, TSource>(byte* sourcePointer, int elementCount, int stride)
    ///        where TConverter : IConverter<TSource, Vector4>, IConverter<Vector4, TSource>
    ///        where TSource : unmanaged
    ///    {
    ///        for (byte* end = sourcePointer + elementCount * stride; sourcePointer < end; sourcePointer += stride)
    ///        {
    ///            TConverter.Convert(*(TSource*)sourcePointer, out var val);
    ///            val *= (Vector4)Color;
    ///            TConverter.Convert(val, out *(TSource*)sourcePointer);
    ///        }
    ///    }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public interface IWriter<TDest>
    {
        /// <param name="sourcePointer">Points to the first element in the vertex buffer, read it as a TSource* to retrieve its value</param>
        /// <param name="elementCount">The amount of vertices. This is not equivalent to the size of the vertex buffer, or the size in bytes taken by individual vertices</param>
        /// <param name="stride">The size in bytes taken by individual vertices, add it to <paramref name="sourcePointer"/> to point to the next element</param>
        /// <typeparam name="TConverter">A helper to convert between <typeparamref name="TSource"/> and <typeparamref name="TDest"/> properly</typeparam>
        /// <typeparam name="TSource">
        /// The source type this vertex buffer was built with, for example <see cref="Vector2"/> or <see cref="Byte4"/>,
        /// use <typeparamref name="TConverter"/> to convert it into a <typeparamref name="TDest"/>.
        /// </typeparam>
        /// <inheritdoc cref="IWriter{TDest}"/>
        unsafe void Write<TConverter, TSource>(byte* sourcePointer, int elementCount, int stride)
            where TConverter : IConverter<TSource, TDest>, IConverter<TDest, TSource> where TSource : unmanaged;
    }
}
