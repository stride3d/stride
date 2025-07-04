// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Diagnostics;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics;

public unsafe partial class GraphicsDevice
{
    /// <summary>
    ///   Allocate descriptor handles.
    /// </summary>
    internal class DescriptorAllocator(GraphicsDevice device, DescriptorHeapType descriptorHeapType) : IDisposable
    {
        // TODO: For now this is a simple bump alloc, but at some point we will have to make a real allocator with free

        private const int DescriptorPerHeap = 256;

        private readonly GraphicsDevice device = device;
        private readonly DescriptorHeapType descriptorHeapType = descriptorHeapType;

        // Cached size of a descriptor handle for the given heap type
        private readonly int descriptorSize = (int) device.NativeDevice.GetDescriptorHandleIncrementSize(descriptorHeapType);

        private ComPtr<ID3D12DescriptorHeap> currentHeap;
        private CpuDescriptorHandle currentHandle;
        private int remainingHandles;


        /// <inheritdoc/>
        public void Dispose()
        {
            SafeRelease(ref currentHeap);
        }


        /// <summary>
        ///   Allocates a specified number of Descriptors from the current descriptor heap.
        /// </summary>
        /// <param name="count">
        ///   The number of Descriptors to allocate. The default value is 1. Must be a positive integer.</param>
        /// <returns>A <see cref="CpuDescriptorHandle"/> pointing to the first allocated Descriptor.</returns>
        /// <remarks>
        ///   If the current Descriptor heap does not have enough remaining handles to satisfy the
        ///   allocation, a new Descriptor heap is automatically created.
        /// </remarks>
        public CpuDescriptorHandle Allocate(int count = 1)
        {
            Debug.Assert(count > 0, "Count must be a positive integer.");

            // If no current heap or not enough remaining handles, create a new heap
            if (currentHeap.IsNull() || remainingHandles < count)
            {
               CreateNewHeap();
            }

            // The handle we return, pointing to the first allocated descriptor
            var resultHandle = currentHandle;
            // Move the current handle forward by the size of the allocated descriptors
            currentHandle.Ptr += (nuint) (descriptorSize * count);
            remainingHandles -= count;

            return resultHandle;

            //
            // Creates a new Descriptor heap.
            //
            void CreateNewHeap()
            {
                Debug.Assert(device is not null, "Graphics Device must not be null.");

                var descriptorHeapDesc = new DescriptorHeapDesc
                {
                    Flags = DescriptorHeapFlags.None,
                    Type = descriptorHeapType,
                    NumDescriptors = DescriptorPerHeap,
                    NodeMask = 1
                };

                HResult result = device.NativeDevice.CreateDescriptorHeap(in descriptorHeapDesc, out ComPtr<ID3D12DescriptorHeap> descriptorHeap);

                if (result.IsFailure)
                    result.Throw();

                currentHeap = descriptorHeap; // TODO: What do we do with the previous heap? Should we release it?

                remainingHandles = DescriptorPerHeap;
                currentHandle = descriptorHeap.GetCPUDescriptorHandleForHeapStart();
            }
        }
    }
}

#endif
