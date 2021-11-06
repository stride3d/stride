// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        internal Silk.NET.Vulkan.QueryPool NativeQueryPool;

        public unsafe bool TryGetData(long[] dataArray)
        {
            fixed (long* dataPointer = &dataArray[0])
            {
                // Read back all results
                var result = GetApi().GetQueryPoolResults(GraphicsDevice.NativeDevice, NativeQueryPool, 0, (uint)QueryCount, (uint)QueryCount * 8, dataPointer, 8, QueryResultFlags.QueryResult64Bit);

                // Some queries are not ready yet
                if (result == Result.NotReady)
                    return false;
            }

            return true;
        }

        private unsafe void Recreate()
        {
            var createInfo = new QueryPoolCreateInfo
            {
                SType = StructureType.QueryPoolCreateInfo,
                QueryCount = (uint)QueryCount,
            };

            switch (QueryType)
            {
                case QueryType.Timestamp:
                    createInfo.QueryType = Silk.NET.Vulkan.QueryType.Timestamp;
                    break;

                default:
                    throw new NotImplementedException();
            }

            GetApi().CreateQueryPool(GraphicsDevice.NativeDevice, &createInfo, null, out NativeQueryPool);
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            GraphicsDevice.Collect(NativeQueryPool);
            NativeQueryPool = new Silk.NET.Vulkan.QueryPool(0);

            base.OnDestroyed();
        }
    }
}
#endif
