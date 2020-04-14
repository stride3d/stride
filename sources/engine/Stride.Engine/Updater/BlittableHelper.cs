// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Xenko.Updater
{
    /// <summary>
    /// Various helpers for blittable types.
    /// </summary>
    // TODO: We should switch to something determined at compile time with assembly processor?
    internal static class BlittableHelper
    {
        private static Dictionary<Type, bool> blittableTypesCache = new Dictionary<Type, bool>();

        // TODO: Performance: precompute this in AssemblyProcessor
        public static bool IsBlittable(Type type)
        {
            lock (blittableTypesCache)
            {
                bool blittable;
                try
                {
                    // Check cache
                    if (blittableTypesCache.TryGetValue(type, out blittable))
                        return blittable;

                    // Class test
                    if (!type.GetTypeInfo().IsValueType)
                    {
                        blittable = false;
                    }
                    else
                    {
                        // Non-blittable types cannot allocate pinned handle
                        GCHandle.Alloc(Activator.CreateInstance(type), GCHandleType.Pinned).Free();
                        blittable = true;
                    }
                }
                catch
                {
                    blittable = false;
                }

                // Register it for next time
                blittableTypesCache[type] = blittable;
                return blittable;
            }
        }
    }
}
