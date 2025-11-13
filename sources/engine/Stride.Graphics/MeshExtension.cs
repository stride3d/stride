// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics.Data;

namespace Stride.Graphics;

public static class MeshExtension
{
    /// <summary>
    /// Fetch this buffer and create a helper to read from it.
    /// </summary>
    /// <remarks>
    /// This operation loads the buffer from disk, or directly from the gpu. It is <b>very slow</b>, avoid calling this too often if at all possible.
    /// </remarks>
    /// <param name="binding"> The bindings for this buffer </param>
    /// <param name="services"> The service used to retrieve the buffer from disk/GPU if it wasn't found through other means </param>
    /// <param name="helper"> The helper class to interact with the loaded buffer </param>
    /// <param name="count"> The amount of vertices this buffer holds </param>
    /// <inheritdoc cref="VertexBufferHelper"/>
    public static void AsReadable(this VertexBufferBinding binding, IServiceRegistry services, out VertexBufferHelper helper, out int count)
    {
        helper = new VertexBufferHelper(binding, services, out count);
    }

    /// <summary>
    /// Fetch this buffer and create a helper to read from it.
    /// </summary>
    /// <remarks>
    /// This operation loads the buffer from disk, or directly from the gpu. It is <b>very slow</b>, avoid calling this too often if at all possible.
    /// </remarks>
    /// <param name="binding"> The bindings for this buffer </param>
    /// <param name="services"> The service used to retrieve the buffer from disk/GPU if it wasn't found through other means </param>
    /// <param name="helper"> The helper class to interact with the loaded buffer </param>
    /// <param name="count"> The amount of indices this buffer holds </param>
    /// <inheritdoc cref="IndexBufferHelper"/>
    public static void AsReadable(this IndexBufferBinding binding, IServiceRegistry services, out IndexBufferHelper helper, out int count)
    {
        helper = new IndexBufferHelper(binding, services, out count);
    }

    /// <summary>
    /// Given a semantic and its index, returns its offset and size in the given vertex buffer. Similar to <see cref="VertexDeclaration.EnumerateWithOffsets"/>
    /// </summary>
    public static bool TryGetElement(this VertexDeclaration declaration, string vertexElementUsage, int semanticIndex, out VertexElementWithOffset result)
    {
        int offset = 0;
        foreach (var element in declaration.VertexElements)
        {
            // Get new offset (if specified)
            var currentElementOffset = element.AlignedByteOffset;
            if (currentElementOffset != VertexElement.AppendAligned)
                offset = currentElementOffset;

            var elementSize = element.Format.SizeInBytes;
            if (vertexElementUsage == element.SemanticName && semanticIndex == element.SemanticIndex)
            {
                result = new VertexElementWithOffset(element, offset, elementSize);
                return true;
            }

            // Compute next offset (if automatic)
            offset += elementSize;
        }

        result = default;
        return false;
    }

    /// <inheritdoc cref="TryFetchBufferContent"/>
    /// <remarks> Same as <see cref="TryFetchBufferContent"/> but throws on failure </remarks>
    internal static byte[] FetchBufferContentOrThrow(Buffer buffer, IServiceRegistry services)
    {
        var data = TryFetchBufferContent(buffer, services);
        if (data is null || data.Length == 0)
        {
            throw new InvalidOperationException(
                $"Failed to find mesh buffers while attempting to {nameof(FetchBufferContentOrThrow)}. " +
                $"Make sure that the mesh is either an asset on disk, or has its buffer data attached to the buffer through '{nameof(AttachedReference)}'\n");
        }

        return data;
    }

    /// <summary> Get buffer content from GPU, shared memory or disk </summary>
    internal static byte[]? TryFetchBufferContent(Buffer buffer, IServiceRegistry services)
    {
        var bufRef = AttachedReferenceManager.GetAttachedReference(buffer);
        if (bufRef?.Data != null && ((BufferData)bufRef.Data).Content is { } output)
            return output;

        // Try to load it from disk, a file provider is required, editor does not provide one
        if (bufRef?.Url != null && services.GetService<IDatabaseFileProviderService>() is {} provider && provider.FileProvider is not null)
        {
            // We have to create a new one without providing services to ensure that it dumps the graphics buffer data to the attached reference below
            var cleanManager = new ContentManager(provider);
            var bufferCopy = cleanManager.Load<Buffer>(bufRef.Url);
            try
            {
                return bufferCopy.GetSerializationData().Content;
            }
            finally
            {
                cleanManager.Unload(bufRef.Url);
            }
        }

        // When the mesh is created at runtime, or when the file provider is null as can be the case in editor, fetch from GPU
        // will most likely break on non-dx11 APIs
        if (services.GetService<GraphicsContext>() is { } context)
        {
            output = new byte[buffer.SizeInBytes];
            if (buffer.Description.Usage == GraphicsResourceUsage.Staging) // Directly if this is a staging resource
            {
                buffer.GetData(context.CommandList, buffer, output);
            }
            else // inefficient way to use the Copy method using dynamic staging texture
            {
                using var throughStaging = buffer.ToStaging();
                buffer.GetData(context.CommandList, throughStaging, output);
            }

            return output;
        }

        return null;
    }
}
