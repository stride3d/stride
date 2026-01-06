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

    /// <summary>
    ///   Gets the internal Direct3D 11 Queries.
    /// </summary>
    /// <remarks>
    ///   If any of the references is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
    ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    /// </remarks>
    internal ReadOnlySpan<ComPtr<ID3D11Query>> NativeQueries => nativeQueries;


    /// <summary>
    ///   Attempts to retrieve data from the in-flight GPU queries.
    /// </summary>
    /// <param name="dataArray">
    ///   An array of <see langword="long"/> values to be populated with the retrieved data. The array must have a length
    ///   equal to the number of queries performed (<see cref="QueryCount"/>).
    /// </param>
    /// <returns><see langword="true"/> if all data queries succeed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    ///   This method tries to perform reads for the multiple GPU queries in the pool and populates the provided array
    ///   with the results. If any query fails, the method returns <see langword="false"/> and the array may contain
    ///   partial or uninitialized data.
    /// </remarks>
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

        return true;
    }

    /// <inheritdoc/>
    protected internal override void OnDestroyed(bool immediately = false)
    {
        for (var i = 0; i < QueryCount; i++)
        {
            ComPtrHelpers.SafeRelease(ref nativeQueries[i]);
        }
        nativeQueries = null;

        base.OnDestroyed(immediately);
    }

    /// <summary>
    ///   Implementation in Direct3D 11 that recreates the queries in the pool.
    /// </summary>
    /// <exception cref="NotImplementedException">
    ///   Only GPU queries of type <see cref="QueryType.Timestamp"/> are supported.
    /// </exception>
    private unsafe partial void Recreate()
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
