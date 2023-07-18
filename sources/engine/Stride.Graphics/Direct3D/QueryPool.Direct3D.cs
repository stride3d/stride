// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Stride.Graphics
{
    public unsafe partial class QueryPool
    {
        internal QueryPtr[] NativeQueries;

        public bool TryGetData(long[] dataArray)
        {
            var deviceContext = GraphicsDevice.NativeDeviceContext;

            for (var index = 0; index < NativeQueries.Length; index++)
            {
                var query = (ID3D11Asynchronous*) NativeQueries[index].Query;

                HResult result = deviceContext->GetData(query, ref dataArray[index], sizeof(long), GetDataFlags: 0);

                if (result.IsFailure)
                    return false;
            }

            return true;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            for (var i = 0; i < QueryCount; i++)
            {
                NativeQueries[i].Query->Release();
                NativeQueries[i] = null;
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
                    queryDescription.Query = Query.Timestamp;
                    break;

                default:
                    throw new NotImplementedException();
            }

            NativeQueries = new QueryPtr[QueryCount];
            for (var i = 0; i < QueryCount; i++)
            {
                ID3D11Query* query;
                HResult result = NativeDevice->CreateQuery(in queryDescription, &query);

                if (result.IsFailure)
                    result.Throw();

                NativeQueries[i] = query;
            }
        }

        #region QueryPtr structure

        // Ancillary struct to store a Query without messing with its reference count (as ComPtr<T> does).
        internal readonly record struct QueryPtr(ID3D11Query* Query)
        {
            public static implicit operator ID3D11Query*(QueryPtr queryPtr) => queryPtr.Query;
            public static implicit operator QueryPtr(ID3D11Query* query) => new(query);
        }

        #endregion
    }
}

#endif
