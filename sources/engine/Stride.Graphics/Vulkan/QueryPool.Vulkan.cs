// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

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
                var result = vkGetQueryPoolResults(GraphicsDevice.NativeDevice, NativeQueryPool, 0, (uint)QueryCount, (uint)QueryCount * 8, dataPointer, 8, VkQueryResultFlags._64);

                // Some queries are not ready yet
                if (result == VkResult.NotReady)
                    return false;
            }

            return true;
        }

        private unsafe void Recreate()
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

            vkCreateQueryPool(GraphicsDevice.NativeDevice, &createInfo, null, out NativeQueryPool);
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            GraphicsDevice.Collect(NativeQueryPool);
            NativeQueryPool = VkQueryPool.Null;

            base.OnDestroyed();
        }
    }
}
#endif
