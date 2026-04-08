// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.UnsafeExtensions;
using Stride.Graphics.Data;

namespace Stride.Graphics
{
    /// <summary>
    ///   All-in-one GPU Buffer that is able to represent many types of Buffers (shader Constant Buffers, Structured Buffers,
    ///   Raw Buffers, Argument Buffers, etc.).
    /// </summary>
    /// <remarks>
    ///   <para><see cref="Buffer"/> constains static methods for creating new Buffers by specifying all their characteristics.</para>
    ///   <para>
    ///     Also look for the following static methods that aid in the creation of specific kinds of Buffers:
    ///     <see cref="Buffer.Argument"/> (for <strong>Argument Buffers</strong>), <see cref="Buffer.Constant"/> (for <strong>Constant Buffers</strong>),
    ///     <see cref="Buffer.Index"/> (for <strong>Index Buffers</strong>), <see cref="Buffer.Raw"/> (for <strong>Raw Buffers</strong>),
    ///     <see cref="Buffer.Structured"/> (for <strong>Structured Buffers</strong>), <see cref="Buffer.Typed"/> (for <strong>Typed Buffers</strong>),
    ///     and <see cref="Buffer.Vertex"/> (for <strong>Vertex Buffers</strong>).
    ///   </para>
    ///   <para>Consult the documentation of your graphics API for more information on each kind of Buffer.</para>
    /// </remarks>
    /// <seealso cref="Buffer{T}"/>
    /// <seealso cref="Buffer.Argument"/>
    /// <seealso cref="Buffer.Constant"/>
    /// <seealso cref="Buffer.Index"/>
    /// <seealso cref="Buffer.Raw"/>
    /// <seealso cref="Buffer.Structured"/>
    /// <seealso cref="Buffer.Typed"/>
    /// <seealso cref="Buffer.Vertex"/>
    [DataSerializer(typeof(BufferSerializer))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Buffer>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<Buffer>))]
    public partial class Buffer : GraphicsResource
    {
        protected int elementCount;
        private BufferDescription bufferDescription;


        public Buffer() { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Buffer"/> class.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> the Buffer belongs to.</param>
        protected Buffer(GraphicsDevice device) : base(device) { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Buffer"/> class.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> the Buffer belongs to.</param>
        /// <param name="name">
        ///   A string to use as a name for identifying the Buffer. Useful when debugging.
        ///   Specify <see langword="null"/> to use the type's name instead.
        /// </param>
        protected Buffer(GraphicsDevice device, string? name) : base(device, name) { }


        /// <summary>
        ///   Gets a description of the Buffer.
        /// </summary>
        public BufferDescription Description => bufferDescription;

        /// <summary>
        ///   Gets a value that indicates how the Buffer is to be read from and written to.
        /// </summary>
        public GraphicsResourceUsage Usage => bufferDescription.Usage;

        /// <summary>
        ///   Gets a combination of flags describing the type of the Buffer.
        /// </summary>
        public BufferFlags Flags => bufferDescription.BufferFlags;

        /// <summary>
        ///   Gets the size of the Buffer in bytes.
        /// </summary>
        public int SizeInBytes => bufferDescription.SizeInBytes;

        /// <summary>
        ///   Gets the size of the structure (in bytes) when the Buffer represents a typed / structured buffer.
        /// </summary>
        public int StructureByteStride => bufferDescription.StructureByteStride;

        /// <summary>
        ///   Gets the number of elements in the Buffer.
        /// </summary>
        /// <remarks>
        ///   This value is valid for Structured Buffers, Raw Buffers, and Index Buffers that are used as a Shared Resource View.
        /// </remarks>
        public int ElementCount
        {
            get => elementCount;
            protected set => elementCount = value;
        }

        /// <summary>
        ///   Gets a combination of flags describing how a View over the Buffer should behave.
        /// </summary>
        public BufferFlags ViewFlags { get; private set; }

        /// <summary>
        ///   Gets the format of the elements of the Buffer as interpreted through a View.
        /// </summary>
        public PixelFormat ViewFormat { get; private set; }

        /// <summary>
        ///   Gets or sets the initial Append / Consume Buffer counter offset.
        /// </summary>
        /// <value>
        ///    A value of -1 indicates the current offset should be kept.
        ///    Any other values set the hidden counter for that Appendable / Consumable Buffer.
        ///    The default value is -1.
        /// </value>
        /// <remarks>
        ///   This value is only relevant for Buffers which have the <see cref="BufferFlags.StructuredAppendBuffer"/> or
        ///   <see cref="BufferFlags.StructuredCounterBuffer"/> flags, otherwise it is ignored.
        /// </remarks>
        public int InitialCounterOffset { get; set; } = -1;


        /// <summary>
        ///   Returns a staging Buffer that can be read / written by the CPU that is equivalent to the Buffer.
        /// </summary>
        /// <returns>A new instance of the Buffer as a staging resource.</returns>
        public Buffer ToStaging()
        {
            var stagingDesc = Description with { Usage = GraphicsResourceUsage.Staging, BufferFlags = BufferFlags.None };

            var buffer = GraphicsDevice.IsDebugMode
                ? new Buffer(GraphicsDevice, GetDebugName(in stagingDesc, elementType: null, ElementCount))
                : new Buffer(GraphicsDevice, Name);

            return buffer.InitializeFromImpl(in stagingDesc, BufferFlags.None, ViewFormat, dataPointer: IntPtr.Zero);
        }

        /// <summary>
        ///   Returns a new Buffer with exactly the same characteristics as the Buffer, but does not copy its contents.
        /// </summary>
        /// <returns>A clone of the Buffer.</returns>
        public Buffer Clone()
        {
            return new Buffer(GraphicsDevice, Name).InitializeFromImpl(in bufferDescription, ViewFlags, ViewFormat, dataPointer: IntPtr.Zero);
        }

        #region Initialization

        /// <summary>
        ///   Initializes this <see cref="Buffer"/> instance with the provided options.
        /// </summary>
        /// <param name="description">A <see cref="BufferDescription"/> structure describing the Buffer characteristics.</param>
        /// <param name="viewFlags">A combination of flags determining how the Views over the Buffer should behave.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="dataPointer">The data pointer to the data to initialize the Buffer with.</param>
        /// <returns>This same instance of <see cref="Buffer"/> already initialized.</returns>
        protected partial Buffer InitializeFromImpl(ref readonly BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer);

        #endregion

        #region Debug

        /// <summary>
        ///   Generates a debug-friendly name for the Buffer based on its usage, flags, and size.
        /// </summary>
        /// <param name="bufferDescription">The description of the Buffer.</param>
        /// <param name="elementType">The name of the type of the elements in the Buffer.</param>
        /// <param name="elementSize">The size in bytes of an element in the Buffer.</param>
        /// <returns>A string representing the debug name of the Buffer.</returns>
        private static string GetDebugName(ref readonly BufferDescription bufferDescription, string? elementType = null, int elementSize = 0)
        {
            return GetDebugName(bufferDescription.Usage, bufferDescription.BufferFlags, bufferDescription.SizeInBytes, elementType, elementSize);
        }

        /// <summary>
        ///   Generates a debug-friendly name for the Buffer based on its usage, flags, and size.
        /// </summary>
        /// <param name="bufferDescription">The description of the Buffer.</param>
        /// <param name="elementType">The name of the type of the elements in the Buffer.</param>
        /// <param name="elementSize">The size in bytes of an element in the Buffer.</param>
        /// <returns>A string representing the debug name of the Buffer.</returns>
        private static string GetDebugName(GraphicsResourceUsage bufferUsage, BufferFlags bufferFlags, int sizeInBytes, string? elementType = null, int elementSize = 0)
        {
            var usage = bufferUsage != GraphicsResourceUsage.Default
                ? $"{bufferUsage} "
                : string.Empty;

            var flags = bufferFlags switch
            {
                BufferFlags.ConstantBuffer => "Constant Buffer",
                BufferFlags.IndexBuffer => "Index Buffer",
                BufferFlags.VertexBuffer => "Vertex Buffer",
                BufferFlags.ArgumentBuffer => "Argument Buffer",
                BufferFlags.RawBuffer => "Raw Buffer",
                BufferFlags.StructuredAppendBuffer => "Structured Append Buffer",
                BufferFlags.StructuredCounterBuffer => "Structured Counter Buffer",

                _ => "Buffer"
            };
            var typeOfElement = elementType is not null ? $" of {elementType}" : string.Empty;
            var elementCount = elementSize > 0 && elementSize < sizeInBytes
                ? sizeInBytes / elementSize
                : 0;
            var elements = elementCount > 0 ? $", {elementCount} elements" : string.Empty;

            return $"{usage}{flags}{typeOfElement} ({sizeInBytes} bytes{elements})";
        }

        #endregion

        #region GetData: Reading data from the Buffer

        /// <summary>
        ///   Gets the contents of the Buffer as an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the data to read from the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <returns>An array of data with the contents of the Buffer.</returns>
        /// <remarks>
        ///   This method only works when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        ///   <para>
        ///     This method creates internally a staging resource (if this <see cref="Buffer"/> is not already a staging resource),
        ///     copies to it and map it to memory. Use a method that allows to specify an explicit staging resource for optimal performance.
        ///   </para>
        /// </remarks>
        public unsafe TData[] GetData<TData>(CommandList commandList) where TData : unmanaged
        {
            var toData = new TData[SizeInBytes / sizeof(TData)];
            GetData(commandList, toData);
            return toData;
        }

        /// <summary>
        ///   Copies the contents of the Buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the data to read from the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="toData">The destination array where to copy the Buffer contents.</param>
        /// <exception cref="ArgumentException">
        ///   The length of the destination data buffer (<paramref name="toData"/>) is larger than the size of the Buffer.
        /// </exception>
        /// <remarks>
        ///   This method only works when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        ///   <para>
        ///     This method creates internally a staging resource (if this <see cref="Buffer"/> is not already a staging resource),
        ///     copies to it and map it to memory. Use a method that allows to specify an explicit staging resource for optimal performance.
        ///   </para>
        /// </remarks>
        public void GetData<TData>(CommandList commandList, TData[] toData) where TData : unmanaged
        {
            // Get data from this resource
            if (Description.Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                GetData(commandList, this, toData);
            }
            else
            {
                // Inefficient way to use the Copy method using dynamic staging Buffer
                using var throughStaging = ToStaging();
                GetData(commandList, throughStaging, toData);
            }
        }

        /// <summary>
        ///   Gets a single data element of the Buffer.
        /// </summary>
        /// <typeparam name="TData">The type of the data to read from the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="toData">When this method returns, contains the element read from the Buffer.</param>
        /// <exception cref="ArgumentException">
        ///   The length of the destination data buffer (<paramref name="toData"/>) is larger than the size of the Buffer.
        /// </exception>
        /// <remarks>
        ///   This method only works when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        ///   <para>
        ///     This method creates internally a staging resource (if this <see cref="Buffer"/> is not already a staging resource),
        ///     copies to it and map it to memory. Use a method that allows to specify an explicit staging resource for optimal performance.
        ///   </para>
        /// </remarks>
        public void GetData<TData>(CommandList commandList, ref TData toData) where TData : unmanaged
        {
            // Get data from this resource
            if (Description.Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                GetData(commandList, this, ref toData);
            }
            else
            {
                // Inefficient way to use the Copy method using dynamic staging Buffer
                using var throughStaging = ToStaging();
                GetData(commandList, throughStaging, ref toData);
            }
        }

        /// <summary>
        ///   Copies a single data element of the Buffer from GPU memory to data on CPU memory using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the data to read from the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="stagingBuffer">The staging buffer used to transfer the data from GPU memory.</param>
        /// <param name="toData">When this method returns, contains the element read from the Buffer.</param>
        /// <exception cref="ArgumentException">
        ///   The length of the destination data buffer (<paramref name="toData"/>) is larger than the size of the Buffer.
        /// </exception>
        /// <remarks>
        ///   This method only works when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public void GetData<TData>(CommandList commandList, Buffer stagingBuffer, ref TData toData) where TData : unmanaged
        {
            GetData(commandList, stagingBuffer, MemoryMarshal.CreateSpan(ref toData, 1));
        }

        /// <summary>
        ///   Copies data from the Buffer from GPU memory into an array on CPU memory using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the data to read from the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="stagingBuffer">The staging buffer used to transfer the data from GPU memory.</param>
        /// <param name="toData">Array where the read data should be copied.</param>
        /// <exception cref="ArgumentException">
        ///   The length of the destination data buffer (<paramref name="toData"/>) is larger than the size of the Buffer.
        /// </exception>
        /// <remarks>
        ///   This method only works when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public void GetData<TData>(CommandList commandList, Buffer stagingBuffer, TData[] toData) where TData : unmanaged
        {
            GetData(commandList, stagingBuffer, toData.AsSpan());
        }

        /// <summary>
        ///   Copies the contents of the Buffer from GPU memory to a CPU memory pointer using a specific staging resource.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="stagingBuffer">The staging buffer used to transfer the data from GPU memory.</param>
        /// <param name="toData">To destination data pointer.</param>
        /// <exception cref="ArgumentException">
        ///   The length of the destination data buffer (<paramref name="toData"/>) is larger than the size of the Buffer.
        /// </exception>
        /// <remarks>
        ///   This method only works when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        [Obsolete("This method is obsolete. Use the Span-based methods instead")]
        public unsafe void GetData(CommandList commandList, Buffer stagingBuffer, DataPointer toData)
        {
            GetData(commandList, stagingBuffer, new Span<byte>((void*) toData.Pointer, toData.Size));
        }

        /// <summary>
        ///   Copies the content of the Buffer from GPU memory to a CPU memory pointer using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the data to read from the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="stagingBuffer">The staging buffer used to transfer the data from GPU memory.</param>
        /// <param name="toData">To destination span where the read data will be written.</param>
        /// <exception cref="ArgumentException">
        ///   The length of the destination data buffer (<paramref name="toData"/>) is larger than the size of the Buffer.
        /// </exception>
        /// <remarks>
        ///   This method only works when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe void GetData<TData>(CommandList commandList, Buffer stagingBuffer, Span<TData> toData) where TData : unmanaged
        {
            // Check destination buffer has valid size
            int toDataSizeInBytes = toData.Length * sizeof(TData);
            if (toDataSizeInBytes > SizeInBytes)
                throw new ArgumentException("The length of the destination data buffer is larger than the size of the Buffer");

            // Copy the Buffer to a staging resource
            if (!ReferenceEquals(this, stagingBuffer))
                commandList.Copy(this, stagingBuffer);

            // Map the staging resource to CPU-readable memory
            var mappedResource = commandList.MapSubResource(stagingBuffer, subResourceIndex: 0, MapMode.Read);
            var fromData = new ReadOnlySpan<TData>((void*) mappedResource.DataBox.DataPointer, toData.Length);
            fromData.CopyTo(toData);
            //Utilities.CopyWithAlignmentFallback(pointer, (void*)mappedResource.DataBox.DataPointer, (uint)toDataInBytes);
            commandList.UnmapSubResource(mappedResource);
        }

        #endregion

        #region SetData: Writing data into the Buffer

        /// <summary>
        ///   Copies the contents an array of data on CPU memory into the Buffer in GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the data to write into the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="offsetInBytes"/> is only supported for Buffers declared with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
        /// </remarks>
        public void SetData<TData>(CommandList commandList, ref readonly TData fromData, int offsetInBytes = 0) where TData : unmanaged
        {
            SetData(commandList, MemoryMarshal.CreateReadOnlySpan(in fromData, 1), offsetInBytes);
        }

        /// <summary>
        ///   Copies the contents of an array of data on CPU memory into the Buffer in GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the data to write into the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The array of data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes from the start of the Buffer where data is to be written.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="offsetInBytes"/> is only supported for Buffers declared with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
        /// </remarks>
        public void SetData<TData>(CommandList commandList, TData[] fromData, int offsetInBytes = 0) where TData : unmanaged
        {
            SetData(commandList, (ReadOnlySpan<TData>) fromData.AsSpan(), offsetInBytes);
        }

        /// <summary>
        ///   Copies data from a pointer to data on CPU memory into the Buffer in GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The pointer to the data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes from the start of the Buffer where data is to be written.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="offsetInBytes"/> is only supported for Buffers declared with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
        /// </remarks>
        [Obsolete("This method is obsolete. Use the Span-based methods instead")]
        public unsafe void SetData(CommandList commandList, DataPointer fromData, int offsetInBytes = 0)
        {
            SetData(commandList, new ReadOnlySpan<byte>((void*) fromData.Pointer, fromData.Size), offsetInBytes);
        }

        /// <summary>
        ///   Copies data from a span of data on CPU memory into the Buffer in GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the data to write into the Buffer.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The span of data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes from the start of the Buffer where data is to be written.</param>
        /// <exception cref="ArgumentException">
        ///   The length of <paramref name="fromData"/> is larger than the size of the Buffer.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="offsetInBytes"/> is only supported for Buffers declared with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
        /// </remarks>
        public unsafe void SetData<TData>(CommandList commandList, ReadOnlySpan<TData> fromData, int offsetInBytes = 0) where TData : unmanaged
        {
            // Check size validity of data to copy from
            var fromDataAsBytes = fromData.AsBytes();
            var fromDataSizeInBytes = fromDataAsBytes.Length;
            if (fromDataAsBytes.Length > SizeInBytes)
                throw new ArgumentException("The length of the source data to upload is larger than the size of the Buffer");

            // If the Buffer is declared as Default usage, we can only use UpdateSubresource, which is not optimal but better than nothing
            if (Description.Usage == GraphicsResourceUsage.Default)
            {
                // Set up the dest region inside the Buffer
                if (Description.BufferFlags.HasFlag(BufferFlags.ConstantBuffer))
                {
                    commandList.UpdateSubResource(this, subResourceIndex: 0, fromDataAsBytes);
                }
                else
                {
                    var destRegion = new ResourceRegion(left: offsetInBytes, top: 0, front: 0, right: offsetInBytes + fromDataSizeInBytes, bottom: 1, back: 1);
                    commandList.UpdateSubResource(this, subResourceIndex: 0, fromDataAsBytes, destRegion);
                }
            }
            else
            {
                if (offsetInBytes > 0)
                    throw new ArgumentException("Offsets are only supported for Textures declared with {nameof(GraphicsResourceUsage)}.{nameof(GraphicsResourceUsage.Default)}", nameof(offsetInBytes));

                // Map the Buffer to CPU-writable memory
                var mappedResource = commandList.MapSubResource(this, subResourceIndex: 0, Usage == GraphicsResourceUsage.Staging ? MapMode.Write : MapMode.WriteDiscard);
                var toData = new Span<TData>((void*) mappedResource.DataBox.DataPointer, fromData.Length);
                fromData.CopyTo(toData);
                //Utilities.CopyWithAlignmentFallback((void*)mappedResource.DataBox.DataPointer, pointer, (uint)sizeInBytes);
                commandList.UnmapSubResource(mappedResource);
            }
        }

        #endregion

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="description">The description of the Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer New(GraphicsDevice device, BufferDescription description, PixelFormat viewFormat = PixelFormat.None)
        {
            var bufferType = description.BufferFlags;

            var buffer = device.IsDebugMode
                ? new Buffer(device, GetDebugName(in description))
                : new Buffer(device);

            return buffer.InitializeFromImpl(in description, bufferType, viewFormat, dataPointer: IntPtr.Zero);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the Buffer in bytes.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, elementSize: 0, bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements the Buffer will contain.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="elementCount">Number of elements of type <typeparamref name="T"/> the Buffer will contain.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer<T> New<T>(GraphicsDevice device, int elementCount, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int elementSize = Unsafe.SizeOf<T>();
            int bufferSize = elementSize * elementCount;

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);

            return new Buffer<T>(device, description, bufferFlags, viewFormat: PixelFormat.None, dataPointer: IntPtr.Zero);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the Buffer in bytes.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, elementSize: 0, bufferFlags, viewFormat, usage);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the Buffer in bytes.</param>
        /// <param name="elementSize">Size of an element in the Buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, int elementSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, elementSize, bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the Buffer in bytes.</param>
        /// <param name="elementSize">Size of an element in the Buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);

            var buffer = device.IsDebugMode
                ? new Buffer(device, GetDebugName(in description, elementType: null, elementSize))
                : new Buffer(device);

            return buffer.InitializeFromImpl(in description, bufferFlags, viewFormat, dataPointer: IntPtr.Zero);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the element the Buffer will contain.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="value">The initial value for the element in the Buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer<T> New<T>(GraphicsDevice device, ref readonly T value, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, in value, bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the element the Buffer will contain.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="value">The initial value for the element in the Buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static unsafe Buffer<T> New<T>(GraphicsDevice device, ref readonly T value, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int sizeOfT = sizeof(T);
            int bufferSize = sizeOfT;
            int elementSize = bufferFlags.HasFlag(BufferFlags.StructuredBuffer) ? sizeOfT : 0;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);

            fixed (T* ptrValue = &value)
                return device.IsDebugMode
                    ? new Buffer<T>(device, description, bufferFlags, viewFormat, (nint) ptrValue, GetDebugName(in description, typeof(T).Name, elementSize))
                    : new Buffer<T>(device, description, bufferFlags, viewFormat, (nint) ptrValue);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements the Buffer will contain.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValue">The initial data the Buffer will contain.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer<T> New<T>(GraphicsDevice device, T[] initialValue, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, (ReadOnlySpan<T>) initialValue.AsSpan(), bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements the Buffer will contain.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValue">The initial value of the Buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer<T> New<T>(GraphicsDevice device, T[] initialValue, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, (ReadOnlySpan<T>) initialValue.AsSpan(), bufferFlags, viewFormat, usage);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements the Buffer will contain.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValues">The initial data the Buffer will contain.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static unsafe Buffer<T> New<T>(GraphicsDevice device, ReadOnlySpan<T> initialValues, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int elementSize = sizeof(T);
            int bufferSize = elementSize * initialValues.Length;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);

            fixed (void* ptrInitialValue = initialValues)
                return device.IsDebugMode
                    ? new Buffer<T>(device, description, bufferFlags, viewFormat, (nint) ptrInitialValue, GetDebugName(in description, typeof(T).Name, elementSize))
                    : new Buffer<T>(device, description, bufferFlags, viewFormat, (nint) ptrInitialValue);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/> from a byte array.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValues">The initial data the Buffer will contain.</param>
        /// <param name="elementSize">
        ///   The size of an element in bytes.
        ///   <list type="bullet">
        ///     <item>For <strong>Index Buffers</strong> this must be equal to 2 (ths size of <see cref="short"/>) or 4 bytes (the size of <see cref="int"/>).</item>
        ///     <item>For <strong>Structured / Typed Buffers</strong> this must be equal to the size of the element <see langword="struct"/>.</item>
        ///     <item>For other types of Buffers, this can be set to 0.</item>
        ///   </list>
        /// </param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> initialValues, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            var buffer = device.IsDebugMode
                ? new Buffer(device, GetDebugName(GraphicsResourceUsage.Default, bufferFlags, initialValues.Length, elementType: null, elementSize))
                : new Buffer(device);

            return buffer.InitializeFrom(initialValues, elementSize, bufferFlags, viewFormat, usage);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="dataPointer">The data pointer to the initial data the Buffer will contain.</param>
        /// <param name="elementSize">
        ///   The size of an element in bytes.
        ///   <list type="bullet">
        ///     <item>For <strong>Index Buffers</strong> this must be equal to 2 (ths size of <see cref="short"/>) or 4 bytes (the size of <see cref="int"/>).</item>
        ///     <item>For <strong>Structured / Typed Buffers</strong> this must be equal to the size of the element <see langword="struct"/>.</item>
        ///     <item>For other types of Buffers, this can be set to 0.</item>
        ///   </list>
        /// </param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        [Obsolete("This method is obsolete. Use the span-based methods instead")]
        public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, dataPointer, elementSize, bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        ///   Creates a new <see cref="Buffer"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="dataPointer">The data pointer to the initial data the Buffer will contain.</param>
        /// <param name="elementSize">
        ///   The size of an element in bytes.
        ///   <list type="bullet">
        ///     <item>For <strong>Index Buffers</strong> this must be equal to 2 (ths size of <see cref="short"/>) or 4 bytes (the size of <see cref="int"/>).</item>
        ///     <item>For <strong>Structured / Typed Buffers</strong> this must be equal to the size of the element <see langword="struct"/>.</item>
        ///     <item>For other types of Buffers, this can be set to 0.</item>
        ///   </list>
        /// </param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new instance of <see cref="Buffer"/>.</returns>
        [Obsolete("This method is obsolete. Use the span-based methods instead")]
        public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            int bufferSize = dataPointer.Size;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);

            var buffer = device.IsDebugMode
                ? new Buffer(device, GetDebugName(in description, elementType: null, elementSize))
                : new Buffer(device);

            return new Buffer(device).InitializeFromImpl(in description, bufferFlags, viewFormat, dataPointer.Pointer);
        }

        /// <summary>
        ///   Initializes this <see cref="Buffer"/> instance with the provided options.
        /// </summary>
        /// <param name="initialValues">The initial data the Buffer will contain.</param>
        /// <param name="elementSize">
        ///   The size of an element in bytes.
        ///   <list type="bullet">
        ///     <item>For <strong>Index Buffers</strong> this must be equal to 2 (ths size of <see cref="short"/>) or 4 bytes (the size of <see cref="int"/>).</item>
        ///     <item>For <strong>Structured / Typed Buffers</strong> this must be equal to the size of the element <see langword="struct"/>.</item>
        ///     <item>For other types of Buffers, this can be set to 0.</item>
        ///   </list>
        /// </param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>This same instance of <see cref="Buffer"/> already initialized.</returns>
        internal unsafe Buffer InitializeFrom(ReadOnlySpan<byte> initialValues, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            int bufferSize = initialValues.Length;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            fixed (void* ptrInitialValue = initialValues)
                return InitializeFromImpl(description, bufferFlags, viewFormat, (nint) ptrInitialValue);
        }

        /// <summary>
        ///   Checks the intended format for a <see cref="Buffer"/> is compatible with its type and flags.
        /// </summary>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="elementSize">
        ///   The size of an element in bytes.
        ///   <list type="bullet">
        ///     <item>For <strong>Index Buffers</strong> this must be equal to 2 (ths size of <see cref="short"/>) or 4 bytes (the size of <see cref="int"/>).</item>
        ///     <item>For <strong>Structured / Typed Buffers</strong> this must be equal to the size of the element <see langword="struct"/>.</item>
        ///     <item>For other types of Buffers, this can be set to 0.</item>
        ///   </list>
        /// </param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <returns>The proposed <see cref="PixelFormat"/> to use.</returns>
        /// <exception cref="ArgumentException">
        ///   The <see cref="Buffer"/> is an <strong>Index Buffer</strong> that will be bound as a <em>Shader Resource</em>,
        ///   but the <paramref name="elementSize"/> is neither 2 bytes (<c>sizeof(short)</c>) nor 4 bytes (<c>sizeof(int)</c>).
        /// </exception>
        private static PixelFormat CheckPixelFormat(BufferFlags bufferFlags, int elementSize, PixelFormat viewFormat)
        {
            if (!bufferFlags.HasFlag(BufferFlags.IndexBuffer) || !bufferFlags.HasFlag(BufferFlags.ShaderResource))
                return viewFormat;

            if (elementSize != 2 && elementSize != 4)
                throw new ArgumentException("Element size must be set to sizeof(short) = 2 or sizeof(int) = 4 for Index Buffers if bound as a Shader Resource", nameof(elementSize));

            return elementSize == 2 ? PixelFormat.R16_UInt : PixelFormat.R32_UInt;
        }

        /// <summary>
        ///   Composes a new <see cref="BufferDescription"/> structure with the provided options.
        /// </summary>
        /// <param name="bufferSize">The size in bytes of the Buffer.</param>
        /// <param name="elementSize">The size in bytes of each element (in case of a <strong>Structured Buffer</strong>).</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <returns>A new <see cref="BufferDescription"/>.</returns>
        private static BufferDescription NewDescription(int bufferSize, int elementSize, BufferFlags bufferFlags, GraphicsResourceUsage usage)
        {
            return new BufferDescription
            {
                SizeInBytes = bufferSize,
                StructureByteStride = bufferFlags.HasFlag(BufferFlags.StructuredBuffer) ? elementSize : 0,
                BufferFlags = bufferFlags,
                Usage = usage
            };
        }

        /// <summary>
        ///   Sets the <see cref="Buffer"/> to be recreated with the specified data whenever the <see cref="GraphicsDevice"/> it depends on is reset.
        /// </summary>
        /// <typeparam name="T">The type of the elements the Buffer will contain.</typeparam>
        /// <param name="data">The data to use to recreate the Buffer with.</param>
        /// <returns>This instance.</returns>
        public Buffer RecreateWith<T>(T[] data) where T : unmanaged
        {
            Reload = (graphicsResource, _) => ((Buffer) graphicsResource).Recreate(data);

            return this;
        }

        /// <summary>
        ///   Sets the <see cref="Buffer"/> to be recreated with the specified data whenever the <see cref="GraphicsDevice"/> it depends on is reset.
        /// </summary>
        /// <param name="dataPointer">The data pointer to the data to use to recreate the Buffer with.</param>
        /// <returns>This instance.</returns>
        public Buffer RecreateWith(IntPtr dataPointer)
        {
            Reload = (graphicsResource, _) => ((Buffer) graphicsResource).Recreate(dataPointer);

            return this;
        }

        /// <summary>
        ///   Recreates the Buffer explicitly with the provided data. Usually called after the <see cref="GraphicsDevice"/> has been reset.
        /// </summary>
        /// <typeparam name="T">The type of the elements the Buffer will contain.</typeparam>
        /// <param name="data">The data to use to recreate the Buffer with.</param>
        public unsafe void Recreate<T>(T[] data) where T : unmanaged
        {
            fixed (void* ptrData = data)
                Recreate((nint) ptrData);
        }
    }

    /// <summary>
    ///   All-in-one GPU buffer that is able to represent many types of Buffers (shader Constant Buffers, Structured Buffers,
    ///   Raw Buffers, Argument Buffers, etc.), but with <strong>typed information</strong>.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the Buffer.</typeparam>
    /// <remarks>
    ///   <para><see cref="Buffer{T}"/> constains static methods for creating new Buffers with typed information by specifying all their characteristics.</para>
    ///   <para>
    ///     Also look for the following static methods that aid in the creation of specific kinds of Buffers:
    ///     <see cref="Buffer.Argument"/> (for <strong>Argument Buffers</strong>), <see cref="Buffer.Constant"/> (for <strong>Constant Buffers</strong>),
    ///     <see cref="Buffer.Index"/> (for <strong>Index Buffers</strong>), <see cref="Buffer.Raw"/> (for <strong>Raw Buffers</strong>),
    ///     <see cref="Buffer.Structured"/> (for <strong>Structured Buffers</strong>), <see cref="Buffer.Typed"/> (for <strong>Typed Buffers</strong>),
    ///     and <see cref="Buffer.Vertex"/> (for <strong>Vertex Buffers</strong>).
    ///   </para>
    ///   <para>You can also check the methods of <see cref="Buffer"/> for creating Buffers with the maximum flexibility.</para>
    ///   <para>Consult the documentation of your graphics API for more information on each kind of Buffer.</para>
    /// </remarks>
    /// <seealso cref="Buffer"/>
    /// <seealso cref="Buffer.Argument"/>
    /// <seealso cref="Buffer.Constant"/>
    /// <seealso cref="Buffer.Index"/>
    /// <seealso cref="Buffer.Raw"/>
    /// <seealso cref="Buffer.Structured"/>
    /// <seealso cref="Buffer.Typed"/>
    /// <seealso cref="Buffer.Vertex"/>
    public class Buffer<T> : Buffer where T : unmanaged
    {
        /// <summary>
        ///   Initializes a new instance of typed <see cref="Buffer{T}"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="description">The description of the Buffer's characteristics.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of Buffer.</param>
        /// <param name="viewFormat">
        ///   View format used if the Buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="dataPointer">The data pointer to the initial data the Buffer will contain.</param>
        /// <param name="name">
        ///   A name for the Buffer, used for debugging purposes.
        ///   Specify <see langword="null"/> to not set a name and use the name of the type instead.
        /// </param>
        protected internal Buffer(GraphicsDevice device,
                                  BufferDescription description, BufferFlags bufferFlags, PixelFormat viewFormat,
                                  IntPtr dataPointer,
                                  string? name = null)
            : base(device, name)
        {
            InitializeFromImpl(in description, bufferFlags, viewFormat, dataPointer);

            ElementSize = Unsafe.SizeOf<T>();
            ElementCount = SizeInBytes / ElementSize;
        }

        /// <summary>
        ///   The size of the elements in this <see cref="Buffer{T}"/> (i.e. the size of <typeparamref name="T"/>).
        /// </summary>
        public readonly int ElementSize;

        /// <summary>
        ///   Gets the contents of the Buffer as an array of data.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <returns>An array of data with the contents of the Buffer.</returns>
        /// <remarks>
        ///   This method only works when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        ///   <para>
        ///     This method creates internally a staging resource (if this <see cref="Buffer"/> is not already a staging resource),
        ///     copies to it and map it to memory. Use a method that allows to specify an explicit staging resource for optimal performance.
        ///   </para>
        /// </remarks>
        public T[] GetData(CommandList commandList)
        {
            return GetData<T>(commandList);
        }

        /// <summary>
        ///   Copies the contents an array of data on CPU memory into the Buffer in GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="offsetInBytes"/> is only supported for Buffers declared with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
        /// </remarks>
        public void SetData(CommandList commandList, ref readonly T fromData, int offsetInBytes = 0)
        {
            base.SetData(commandList, in fromData, offsetInBytes);
        }

        /// <summary>
        ///   Copies the contents of an array of data on CPU memory into the Buffer in GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The array of data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes from the start of the Buffer where data is to be written.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="offsetInBytes"/> is only supported for Buffers declared with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
        /// </remarks>
        public void SetData(CommandList commandList, T[] fromData, int offsetInBytes = 0)
        {
            base.SetData(commandList, fromData, offsetInBytes);
        }
    }
}
