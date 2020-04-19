// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using SharpVulkan;

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        internal SharpVulkan.QueryPool NativeQueryPool;

        public unsafe bool TryGetData(long[] dataArray)
        {
            fixed (long* dataPointer = &dataArray[0])
            {
                // Read back all results
                var result = GraphicsDevice.NativeDevice.GetQueryPoolResults(NativeQueryPool, 0, (uint)QueryCount, QueryCount * 8, new IntPtr(dataPointer), 8, QueryResultFlags.Is64Bits);

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
                StructureType = StructureType.QueryPoolCreateInfo,
                QueryCount = (uint)QueryCount,
            };

            switch (QueryType)
            {
                case QueryType.Timestamp:
                    createInfo.QueryType = SharpVulkan.QueryType.Timestamp;
                    break;

                default:
                    throw new NotImplementedException();
            }

            NativeQueryPool = GraphicsDevice.NativeDevice.CreateQueryPool(ref createInfo);
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            GraphicsDevice.Collect(NativeQueryPool);
            NativeQueryPool = SharpVulkan.QueryPool.Null;

            base.OnDestroyed();
        }
    }
}
#endif
