// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using SharpDX.Direct3D11;

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        internal Query[] NativeQueries;

        public bool TryGetData(long[] dataArray)
        {
            for (var index = 0; index < NativeQueries.Length; index++)
            {
                if (!GraphicsDevice.NativeDeviceContext.GetData(NativeQueries[index], out dataArray[index]))
                    return false;
            }

            return true;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            for (var i = 0; i < QueryCount; i++)
            {
                NativeQueries[i].Dispose();
            }
            NativeQueries = null;

            base.OnDestroyed();
        }

        private void Recreate()
        {
            var queryDescription = new QueryDescription();

            switch (QueryType)
            {
                case QueryType.Timestamp:
                    queryDescription.Type = SharpDX.Direct3D11.QueryType.Timestamp;
                    break;

                default:
                    throw new NotImplementedException();
            }

            NativeQueries = new Query[QueryCount];
            for (var i = 0; i < QueryCount; i++)
            {
                NativeQueries[i] = new Query(NativeDevice, queryDescription);
            }
        }
    }
}

#endif
