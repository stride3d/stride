// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        /// <summary>
        ///   Platform-specific implementation that recreates the queries in the pool.
        /// </summary>
        private unsafe partial void Recreate()
        {
            NullHelper.ToImplement();
        }
    }
}

#endif
