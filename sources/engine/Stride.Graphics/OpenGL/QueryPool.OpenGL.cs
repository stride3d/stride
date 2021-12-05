// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL

using System;

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        internal uint[] NativeQueries;

        public bool TryGetData(long[] dataArray)
        {
#if STRIDE_PLATFORM_IOS
            return false;
#else
            for (var index = 0; index < NativeQueries.Length; index++)
            {
#if STRIDE_GRAPHICS_API_OPENGLES
                GraphicsDevice.GLExtDisjointTimerQuery.GetQueryObject(NativeQueries[index], QueryObjectParameterName.QueryResultAvailable, out long availability);
#else
                GL.GetQueryObject(NativeQueries[index], QueryObjectParameterName.QueryResultAvailable, out long availability);
#endif
                if (availability == 0)
                    return false;

#if STRIDE_GRAPHICS_API_OPENGLES
                GraphicsDevice.GLExtDisjointTimerQuery.GetQueryObject(NativeQueries[index], QueryObjectParameterName.QueryResult, out dataArray[index]);
#else
                GL.GetQueryObject(NativeQueries[index], QueryObjectParameterName.QueryResult, out dataArray[index]);
#endif
            }

            return true;
#endif
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
#if !STRIDE_PLATFORM_IOS
            GL.DeleteQueries((uint)QueryCount, NativeQueries);
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

            NativeQueries = new uint[QueryCount];
#if !STRIDE_PLATFORM_IOS
            GL.GenQueries((uint)QueryCount, NativeQueries);
#endif
        }
    }
}

#endif
