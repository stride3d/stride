// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        internal ComPtr<ID3D11Query>[] NativeQueries;

        public bool TryGetData(long[] dataArray)
        {
            for (var index = 0; index < NativeQueries.Length; index++)
            {
                unsafe
                {
                    var tmp = NativeQueries[index];
                    var a = (ID3D11Asynchronous*)&tmp;
                    fixed( void* pd = &dataArray[index])
                    if (GraphicsDevice.NativeDeviceContext.Get().GetData(a, pd, sizeof(long), 0) != 0)
                        return false;
                }
                
            }

            return true;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            for (var i = 0; i < QueryCount; i++)
            {
                NativeQueries[i].Release();
            }
            NativeQueries = null;

            base.OnDestroyed();
        }

        private void Recreate()
        {
            var queryDescription = new QueryDesc();

            switch (QueryType)
            {
                case QueryType.Timestamp:
                    queryDescription.Query = Query.QueryTimestamp;
                    break;

                default:
                    throw new NotImplementedException();
            }

            NativeQueries = new ComPtr<ID3D11Query>[QueryCount];
            for (var i = 0; i < QueryCount; i++)
            {
                unsafe
                {
                    ID3D11Query* q = null;
                    NativeDevice.Get().CreateQuery(&queryDescription, &q);
                    NativeQueries[i] = new ComPtr<ID3D11Query>(q);
                }
                
            }
        }
    }
}

#endif
