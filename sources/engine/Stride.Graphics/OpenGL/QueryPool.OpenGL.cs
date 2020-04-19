// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL

using System;
#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        internal int[] NativeQueries;

        public bool TryGetData(long[] dataArray)
        {
#if STRIDE_PLATFORM_IOS
            return false;
#else
            for (var index = 0; index < NativeQueries.Length; index++)
            {
#if STRIDE_GRAPHICS_API_OPENGLES
                GL.Ext.GetQueryObject(NativeQueries[index], GetQueryObjectParam.QueryResultAvailable, out long availability);
#else
                GL.GetQueryObject(NativeQueries[index], GetQueryObjectParam.QueryResultAvailable, out long availability);
#endif
                if (availability == 0)
                    return false;

#if STRIDE_GRAPHICS_API_OPENGLES
                GL.Ext.GetQueryObject(NativeQueries[index], GetQueryObjectParam.QueryResult, out dataArray[index]);
#else
                GL.GetQueryObject(NativeQueries[index], GetQueryObjectParam.QueryResult, out dataArray[index]);
#endif
            }

            return true;
#endif
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
#if !STRIDE_PLATFORM_IOS
            GL.DeleteQueries(QueryCount, NativeQueries);
            NativeQueries = null;
#endif
            base.OnDestroyed();
        }

        private void Recreate()
        {
            switch (QueryType)
            {
                case QueryType.Timestamp:
                    break;

                default:
                    throw new NotImplementedException();
            }

            NativeQueries = new int[QueryCount];
#if !STRIDE_PLATFORM_IOS
            GL.GenQueries(QueryCount, NativeQueries);
#endif
        }
    }
}

#endif
