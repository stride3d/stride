// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL 

using System;

namespace Stride.Graphics
{
    /// <summary>
    /// Implementing a new graphic backend requires copying all the content of the Null graphic backend
    /// to a new folder and start implementing all the members. 
    /// To make it easy the default implementation of them in the null backend won't throw
    /// unless you change <see cref="isThrowing"/> to true.
    /// </summary>
    internal static class NullHelper
    {
        private const bool isThrowing = false;

        /// <summary>
        /// Depending on the system configuration, it will do nothing or throw a NotImplementedException.
        /// </summary>
        public static void ToImplement()
        {
            if (isThrowing)
            {
                throw new NotImplementedException();
            }
        }
    }
}

#endif
