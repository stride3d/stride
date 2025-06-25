// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public partial class QueryPool : GraphicsResourceBase
{
    /// <summary>
    /// A pool holding queries with a specific <see cref="QueryType"/>.
    /// </summary>
    public QueryType QueryType { get; }

    public int QueryCount { get; }


    public static QueryPool New(GraphicsDevice graphicsDevice, QueryType queryType, int queryCount)
    {
        /// <summary>
        /// <see cref="QueryType"/> for this pool.
        /// </summary>
        /// <summary>
        /// Capacity of this pool.
        /// </summary>
        /// <summary>
        /// Creates a new <see cref="QueryPool" /> instance.
        /// </summary>
        /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="queryType">The <see cref="QueryType"/> of the pool.</param>
        /// <param name="queryCount">The capacity of the pool.</param>
        /// <returns>An instance of a new <see cref="QueryPool" /></returns>
        /// <inheritdoc/>
        return new QueryPool(graphicsDevice, queryType, queryCount);
    }

    protected QueryPool(GraphicsDevice graphicsDevice, QueryType queryType, int queryCount) : base(graphicsDevice)
    {
        QueryType = queryType;
        QueryCount = queryCount;

        Recreate();
    }

    protected internal override bool OnRecreate()
    {
        base.OnRecreate();

        Recreate();
        return true;
    }

    private partial void Recreate();
}
