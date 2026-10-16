// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Vortice.Vulkan;

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        internal VkQueryPool NativeQueryPool;

        /// <summary>
        ///   Whether the queries must be reset before the next write: Vulkan queries are reset after the pool is created
        ///   and between uses. The Command List records the reset at the first write (see <see cref="CommandList.WriteTimestamp"/>).
        /// </summary>
        internal bool NeedsReset;

        /// <summary>
        ///   The Command List that writes queries of this pool, until it is submitted.
        /// </summary>
        internal CommandList UpdatingCommandList;

        /// <summary>
        ///   The fence value of the last submitted Command List that writes queries of this pool.
        /// </summary>
        internal ulong? CommandListFenceValue;

        /// <summary>
        ///   Attempts to retrieve the data of all the queries in the pool. Every query must have been written.
        /// </summary>
        /// <param name="dataArray">
        ///   An array of <see langword="long"/> values to be populated with the retrieved data. The array must have a length
        ///   equal to the number of queries in the pool (<see cref="QueryCount"/>).
        /// </param>
        /// <returns><see langword="true"/> if all the queries are complete; otherwise, <see langword="false"/>.</returns>
        public unsafe bool TryGetData(long[] dataArray)
        {
            // Until the Command List that resets and writes the queries has run, they still hold the results of their
            // previous use
            if (UpdatingCommandList is not null)
                return false;
            if (CommandListFenceValue is ulong fenceValue && !GraphicsDevice.CommandListFence.IsFenceCompleteInternal(fenceValue))
                return false;

            fixed (long* dataPointer = &dataArray[0])
            {
                // Read back all results
                var result = GraphicsDevice.NativeDeviceApi.vkGetQueryPoolResults(GraphicsDevice.NativeDevice, NativeQueryPool, 0, (uint)QueryCount, (uint)QueryCount * 8, dataPointer, 8, VkQueryResultFlags.Bit64);

                // Some queries are not ready yet
                if (result == VkResult.NotReady)
                    return false;
            }

            // Read: the next write starts a new use of the queries
            NeedsReset = true;
            return true;
        }

        /// <summary>
        ///   Implementation in Vulkan that recreates the queries in the pool.
        /// </summary>
        /// <exception cref="NotImplementedException">
        ///   Only GPU queries of type <see cref="QueryType.Timestamp"/> are supported.
        /// </exception>
        private unsafe partial void Recreate()
        {
            var createInfo = new VkQueryPoolCreateInfo
            {
                sType = VkStructureType.QueryPoolCreateInfo,
                queryCount = (uint)QueryCount,
            };

            switch (QueryType)
            {
                case QueryType.Timestamp:
                    createInfo.queryType = VkQueryType.Timestamp;
                    break;

                default:
                    throw new NotImplementedException();
            }

            GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkCreateQueryPool(GraphicsDevice.NativeDevice, &createInfo, null, out NativeQueryPool));

            NeedsReset = true;
            UpdatingCommandList = null;
            CommandListFenceValue = null;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            GraphicsDevice.Collect(NativeQueryPool);
            NativeQueryPool = VkQueryPool.Null;

            base.OnDestroyed(immediately);
        }
    }
}
#endif
