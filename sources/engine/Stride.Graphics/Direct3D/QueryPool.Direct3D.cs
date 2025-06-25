// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Stride.Graphics;

public unsafe partial class QueryPool
{
    private ComPtr<ID3D11Query>[] nativeQueries;

    internal Span<ComPtr<ID3D11Query>> NativeQueries => nativeQueries;


    public bool TryGetData(long[] dataArray)
    {
        var deviceContext = GraphicsDevice.NativeDeviceContext;

        var queryCount = QueryCount;
        for (var index = 0; index < queryCount; index++)
        {
            HResult result = deviceContext.GetData(nativeQueries[index], ref dataArray[index], sizeof(long), GetDataFlags: 0);

            if (result.IsFailure)
                return false;
        }

        /// <inheritdoc/>
        return true;
    }

    protected internal override void OnDestroyed()
    {
        for (var i = 0; i < QueryCount; i++)
        {
            ComPtrHelpers.SafeRelease(ref nativeQueries[i]);
        }
        nativeQueries = null;

        base.OnDestroyed();
    }

    private partial void Recreate()
    {
        var queryDescription = new QueryDesc
        {
            Query = QueryType switch
            {
                QueryType.Timestamp => Query.Timestamp,

                _ => throw new NotImplementedException($"Query type {QueryType} not supported")
            }
        };

        nativeQueries = new ComPtr<ID3D11Query>[QueryCount];
        for (var i = 0; i < QueryCount; i++)
        {
            ComPtr<ID3D11Query> query = default;
            HResult result = NativeDevice.CreateQuery(in queryDescription, ref query);

            if (result.IsFailure)
                result.Throw();

            nativeQueries[i] = query;
        }
    }
}

#endif
