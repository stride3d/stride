// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Threading
{
    /// <summary>
    /// Helper class to add and remove references to pooled delegates, passed as parameters with <see cref="PooledAttribute"/>>.
    /// </summary>
    internal static class PooledDelegateHelper
    {
        /// <summary>
        /// Adds a reference to a delegate, keeping it from being recycled. Does nothing if the delegate is not drawn from a pool.
        /// </summary>
        /// <param name="pooledDelegate">The pooled delegate</param>
        public static void AddReference([NotNull] Delegate pooledDelegate)
        {
            var closure = pooledDelegate.Target as IPooledClosure;
            closure?.AddReference();
        }

        /// <summary>
        /// Removes a reference from a delegate, allowing it to be recycled. Does nothing if the delegate is not drawn from a pool.
        /// </summary>
        /// <param name="pooledDelegate">The pooled delegate</param>
        public static void Release([NotNull] Delegate pooledDelegate)
        {
            var closure = pooledDelegate.Target as IPooledClosure;
            closure?.Release();
        }
    }
}
