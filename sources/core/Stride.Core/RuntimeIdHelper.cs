// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core
{
    /// <summary>
    /// Extension methods to get a runtime unique id from a live object.
    /// </summary>
    public static class RuntimeIdHelper
    {
        private static readonly ConditionalWeakTable<object, object> RuntimeIds = new ConditionalWeakTable<object, object>();
        private static int count;

        /// <summary>
        /// Computes an unique runtime id (32bits) valid for the specified <see cref="object"/>.
        /// </summary>
        /// <returns>An unique runtime identifier (32bits)</returns>
        public static int ToRuntimeId(object instance)
        {
            if (instance == null)
            {
                return 0;
            }

            lock (RuntimeIds)
            {
                object rtid;
                if (!RuntimeIds.TryGetValue(instance, out rtid))
                {
                    count++;
                    rtid = count;
                    RuntimeIds.Add(instance, rtid);
                }
                return (int)rtid;
            }
        }
    }
}
