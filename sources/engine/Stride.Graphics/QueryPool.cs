// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   A pool holding asynchronous GPU Queries of a specific type.
/// </summary>
/// <seealso cref="Graphics.QueryType"/>
public partial class QueryPool : GraphicsResourceBase
{
    /// <summary>
    ///   Gets the types of asynchronous GPU Queries in the pool.
    /// </summary>
    public QueryType QueryType { get; }

    /// <summary>
    ///   Gets the capacity of the pool.
    /// </summary>
    public int QueryCount { get; }


    /// <summary>
    ///   Creates a new <see cref="QueryPool"/>.
    /// </summary>
    /// <param name="graphicsDevice">The Graphics Device.</param>
    /// <param name="queryType">The type of GPU Queries to contain in the pool.</param>
    /// <param name="queryCount">The capacity of the pool.</param>
    /// <returns>An new instance of <see cref="QueryPool"/> of the specified <paramref name="queryType"/>.</returns>
    public static QueryPool New(GraphicsDevice graphicsDevice, QueryType queryType, int queryCount)
    {
        return new QueryPool(graphicsDevice, queryType, queryCount);
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="QueryPool"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The Graphics Device.</param>
    /// <param name="queryType">The type of GPU Queries to contain in the pool.</param>
    /// <param name="queryCount">The capacity of the pool.</param>
    protected QueryPool(GraphicsDevice graphicsDevice, QueryType queryType, int queryCount) : base(graphicsDevice)
    {
        QueryType = queryType;
        QueryCount = queryCount;

        Recreate();
    }

    /// <inheritdoc/>
    protected internal override bool OnRecreate()
    {
        base.OnRecreate();

        Recreate();
        return true;
    }

    /// <summary>
    ///   Platform-specific implementation that recreates the queries in the pool.
    /// </summary>
    private unsafe partial void Recreate();
}
