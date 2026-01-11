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

        public unsafe bool TryGetData(long[] dataArray)
        {
            fixed (long* dataPointer = &dataArray[0])
            {
                // Read back all results
                var result = GraphicsDevice.NativeDeviceApi.vkGetQueryPoolResults(GraphicsDevice.NativeDevice, NativeQueryPool, 0, (uint)QueryCount, (uint)QueryCount * 8, dataPointer, 8, VkQueryResultFlags.Bit64);

                // Some queries are not ready yet
                if (result == VkResult.NotReady)
                    return false;
            }

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
