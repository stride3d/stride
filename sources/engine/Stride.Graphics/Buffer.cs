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
    /// All-in-One Buffer class linked <see cref="SharpDX.Direct3D11.Buffer"/>.
    /// </summary>
    /// <remarks>
    /// This class is able to create constant buffers, indexelementCountrtex buffers, structured buffer, raw buffers, argument buffers.
    /// </remarks>
    [DataSerializer(typeof(BufferSerializer))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Buffer>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<Buffer>))]
    public partial class Buffer : GraphicsResource
    {
        protected int elementCount;
        private BufferDescription bufferDescription;


        public Buffer() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        protected Buffer(GraphicsDevice device) : base(device) { }


        /// <summary>
        /// Gets the description of this buffer.
        /// </summary>
        public BufferDescription Description => bufferDescription;

        /// <summary>
        /// Value that identifies how the buffer is to be read from and written to.
        /// </summary>
        public GraphicsResourceUsage Usage => bufferDescription.Usage;

        /// <summary>
        /// Buffer flags describing the type of buffer.
        /// </summary>
        public BufferFlags Flags => bufferDescription.BufferFlags;

        /// <summary>
        /// Gets the size of the buffer in bytes.
        /// </summary>
        /// <value>
        /// The size of the buffer in bytes.
        /// </value>
        public int SizeInBytes => bufferDescription.SizeInBytes;

        /// <summary>
        /// The size of the structure (in bytes) when it represents a structured/typed buffer.
        /// </summary>
        public int StructureByteStride => bufferDescription.StructureByteStride;

        /// <summary>
        /// Gets the number of elements.
        /// </summary>
        /// <remarks>
        /// This value is valid for structured buffers, raw buffers and index buffers that are used as a SharedResourceView.
        /// </remarks>
        public int ElementCount
        {
            get => elementCount;
            protected set => elementCount = value;
        }

        /// <summary>
        /// Gets the type of this buffer view.
        /// </summary>
        public BufferFlags ViewFlags { get; private set; }

        /// <summary>
        /// Gets the format of this buffer view.
        /// </summary>
        public PixelFormat ViewFormat { get; private set; }

        /// <summary>
        /// The initial Append/Consume buffer counter offset. A value of -1 indicates the current offset
        /// should be kept. Any other values set the hidden counter for that Appendable/Consumable
        /// Buffer. This value is only relevant for Buffers which have the 'Append' or 'Counter'
        /// flag, otherwise it is ignored. The value get's initialized to -1.
        /// </summary>
        public int InitialCounterOffset { get; set; } = -1;


        /// <summary>
        /// Return an equivalent staging texture CPU read-writable from this instance.
        /// </summary>
        /// <returns>A new instance of this buffer as a staging resource</returns>
        public Buffer ToStaging()
        {
            var stagingDesc = Description with { Usage = GraphicsResourceUsage.Staging, BufferFlags = BufferFlags.None };
            return new Buffer(GraphicsDevice).InitializeFromImpl(stagingDesc, BufferFlags.None, ViewFormat, dataPointer: IntPtr.Zero);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance</returns>
        /// <remarks>
        /// This method will not copy the content of the buffer to the clone
        /// </remarks>
        public Buffer Clone()
        {
            return new Buffer(GraphicsDevice).InitializeFromImpl(in bufferDescription, ViewFlags, ViewFormat, dataPointer: IntPtr.Zero);
        }

        #region Initialization

        /// <summary>
        ///   Initializes this <see cref="Buffer"/> instance with the provided options.
        /// </summary>
        protected partial Buffer InitializeFromImpl(ref readonly BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer);

        #endregion

        #region GetData: Reading data from the Buffer

        /// <summary>
        /// Gets the content of this buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public unsafe TData[] GetData<TData>(CommandList commandList) where TData : unmanaged
        {
            var toData = new TData[SizeInBytes / sizeof(TData)];
            GetData(commandList, toData);
            return toData;
        }

        /// <summary>
        /// Copies the content of this buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="toData">The destination buffer to receive a copy of the texture datas.</param>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
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
                // Unefficient way to use the Copy method using dynamic staging texture
                using var throughStaging = ToStaging();
                GetData(commandList, throughStaging, toData);
            }
        }

        /// <summary>
        /// Copies the content of this buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="toData">The destination buffer to receive a copy of the texture datas.</param>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
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
                // Unefficient way to use the Copy method using dynamic staging texture
                using var throughStaging = ToStaging();
                GetData(commandList, throughStaging, ref toData);
            }
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to an array of data on CPU memory using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="stagingTexture">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data.</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public void GetData<TData>(CommandList commandList, Buffer stagingBuffer, ref TData toData) where TData : unmanaged
        {
            GetData(commandList, stagingBuffer, MemoryMarshal.CreateSpan(ref toData, 1));
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to an array of data on CPU memory using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="stagingTexture">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data.</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public void GetData<TData>(CommandList commandList, Buffer stagingBuffer, TData[] toData) where TData : unmanaged
        {
            GetData(commandList, stagingBuffer, toData.AsSpan());
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public unsafe void GetData(CommandList commandList, Buffer stagingBuffer, DataPointer toData)
        {
            GetData(commandList, stagingBuffer, new Span<byte>((void*) toData.Pointer, toData.Size));
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
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
        /// Copies the content of this buffer from GPU memory to a CPU memory using a specific staging resource.
        /// </summary>
        /// <param name="stagingTexture">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data pointer.</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        [Obsolete("Use span instead")]
        public void SetData<TData>(CommandList commandList, ref readonly TData fromData, int offsetInBytes = 0) where TData : unmanaged
        {
            SetData(commandList, MemoryMarshal.CreateReadOnlySpan(in fromData, 1), offsetInBytes);
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to a CPU memory using a specific staging resource.
        /// </summary>
        /// <param name="stagingTexture">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data pointer.</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public void SetData<TData>(CommandList commandList, TData[] fromData, int offsetInBytes = 0) where TData : unmanaged
        {
            SetData(commandList, (ReadOnlySpan<TData>) fromData.AsSpan(), offsetInBytes);
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">A data pointer.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        [Obsolete("Use span instead")]
        public unsafe void SetData(CommandList commandList, DataPointer fromData, int offsetInBytes = 0)
        {
            SetData(commandList, new ReadOnlySpan<byte>((void*) fromData.Pointer, fromData.Size), offsetInBytes);
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">A data pointer.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
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
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="description">The description of the buffer.</param>
        /// <param name="viewFormat">View format used if the buffer is used as a shared resource view.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, BufferDescription description, PixelFormat viewFormat = PixelFormat.None)
        {
            var bufferType = description.BufferFlags;
            return new Buffer(device).InitializeFromImpl(description, bufferType, viewFormat, dataPointer: IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, elementSize: 0, bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="elementCount">Number of T elment in this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer<T> New<T>(GraphicsDevice device, int elementCount, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int elementSize = Unsafe.SizeOf<T>();
            int bufferSize = elementSize * elementCount;

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            return new Buffer<T>(device, description, bufferFlags, viewFormat: PixelFormat.None, dataPointer: IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, elementSize: 0, bufferFlags, viewFormat, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="elementSize">Size of an element in the buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, int elementSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, elementSize, bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="elementSize">Size of an element in the buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            return new Buffer(device).InitializeFromImpl(description, bufferFlags, viewFormat, dataPointer: IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="value">The initial value of this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer<T> New<T>(GraphicsDevice device, ref readonly T value, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, in value, bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="value">The initial value of this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static unsafe Buffer<T> New<T>(GraphicsDevice device, ref readonly T value, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int sizeOfT = sizeof(T);
            int bufferSize = sizeOfT;
            int elementSize = bufferFlags.HasFlag(BufferFlags.StructuredBuffer) ? sizeOfT : 0;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            fixed (T* ptrValue = &value)
                return new Buffer<T>(device, description, bufferFlags, viewFormat, (nint) ptrValue);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValue">The initial value of this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer<T> New<T>(GraphicsDevice device, T[] initialValue, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, (ReadOnlySpan<T>) initialValue.AsSpan(), bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValue">The initial value of this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer<T> New<T>(GraphicsDevice device, T[] initialValue, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, (ReadOnlySpan<T>)initialValue.AsSpan(), bufferFlags, viewFormat, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValues">The initial data this buffer will contain.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static unsafe Buffer<T> New<T>(GraphicsDevice device, ReadOnlySpan<T> initialValues, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int elementSize = sizeof(T);
            int bufferSize = elementSize * initialValues.Length;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            fixed (void* ptrInitialValue = initialValues)
                return new Buffer<T>(device, description, bufferFlags, viewFormat, (nint) ptrInitialValue);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance from a byte array.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValue">The initial value of this buffer.</param>
        /// <param name="elementSize">Size of an element. Must be equal to 2 or 4 for an index buffer, or to the size of a struct for a structured/typed buffer. Can be set to 0 for other buffers.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> initialValues, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            return new Buffer(device).InitializeFrom(initialValues, elementSize, bufferFlags, viewFormat, usage);
        }
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="dataPointer">The data pointer.</param>
        /// <param name="elementSize">Size of the element.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        [Obsolete("Use span instead")]
        public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, dataPointer, elementSize, bufferFlags, viewFormat: PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="dataPointer">The data pointer.</param>
        /// <param name="elementSize">Size of the element.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        [Obsolete("Use span instead")]
        public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            int bufferSize = dataPointer.Size;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            return new Buffer(device).InitializeFromImpl(description, bufferFlags, viewFormat, dataPointer.Pointer);
        }

        internal unsafe Buffer InitializeFrom(ReadOnlySpan<byte> initialValues, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            int bufferSize = initialValues.Length;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            fixed (void* ptrInitialValue = initialValues)
                return InitializeFromImpl(description, bufferFlags, viewFormat, (nint) ptrInitialValue);
        }
        private static PixelFormat CheckPixelFormat(BufferFlags bufferFlags, int elementSize, PixelFormat viewFormat)
        {
            if (!bufferFlags.HasFlag(BufferFlags.IndexBuffer) || !bufferFlags.HasFlag(BufferFlags.ShaderResource))
                return viewFormat;

            if (elementSize != 2 && elementSize != 4)
                throw new ArgumentException("Element size must be set to sizeof(short) = 2 or sizeof(int) = 4 for Index Buffers if bound as a Shader Resource", nameof(elementSize));

            return elementSize == 2 ? PixelFormat.R16_UInt : PixelFormat.R32_UInt;
        }

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
        /// Reload <see cref="Buffer"/> from given data if <see cref="GraphicsDevice"/> has been reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer">The data pointer.</param>
        /// <returns>This instance.</returns>
        public Buffer RecreateWith<T>(T[] data) where T : unmanaged
        {
            Reload = (graphicsResource, _) => ((Buffer) graphicsResource).Recreate(data);

            return this;
        }

        /// <summary>
        /// Reload <see cref="Buffer"/> from given data if <see cref="GraphicsDevice"/> has been reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer">The data pointer.</param>
        /// <returns>This instance.</returns>
        public Buffer RecreateWith(IntPtr dataPointer)
        {
            Reload = (graphicsResource, _) => ((Buffer) graphicsResource).Recreate(dataPointer);

            return this;
        }

        /// <summary>
        /// Explicitly recreate buffer with given data. Usually called after a <see cref="GraphicsDevice"/> reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer"></param>
        public unsafe void Recreate<T>(T[] data) where T : unmanaged
        {
            fixed (void* ptrData = data)
                Recreate((nint) ptrData);
        }
    }

    /// <summary>
    /// A buffer with typed information.
    /// </summary>
    /// <typeparam name="T">Type of an element of this buffer.</typeparam>
    public class Buffer<T> : Buffer where T : unmanaged
    {
        protected internal Buffer(GraphicsDevice device, BufferDescription description, BufferFlags bufferFlags, PixelFormat viewFormat, IntPtr dataPointer) : base(device)
        {
            InitializeFromImpl(description, bufferFlags, viewFormat, dataPointer);

            ElementSize = Unsafe.SizeOf<T>();
            ElementCount = SizeInBytes / ElementSize;
        }

        /// <summary>
        /// Gets the size of element T.
        /// </summary>
        public readonly int ElementSize;

        /// <summary>
        /// Gets the content of this texture to an array of data.
        /// </summary>
        /// <returns>An array of data.</returns>
        /// <remarks>This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice" />.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public T[] GetData(CommandList commandList)
        {
            return GetData<T>(commandList);
        }

        /// <summary>
        /// Copies the content of a single structure data from CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>. See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public void SetData(CommandList commandList, ref readonly T fromData, int offsetInBytes = 0)
        {
            base.SetData(commandList, in fromData, offsetInBytes);
        }

        /// <summary>
        /// Copies the content an array of data from CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>. See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public void SetData(CommandList commandList, T[] fromData, int offsetInBytes = 0)
        {
            base.SetData(commandList, fromData, offsetInBytes);
        }
    }
}
