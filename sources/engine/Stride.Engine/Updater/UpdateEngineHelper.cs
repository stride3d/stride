// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

namespace Stride.Updater
{
    /// <summary>
    /// Various helper functions for the <see cref="UpdateEngine"/>.
    /// </summary>
    internal static unsafe class UpdateEngineHelper
    {
        public static int ArrayFirstElementOffset = ComputeArrayFirstElementOffset();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ObjectToPtr(object obj)
        {
#if IL
            ldarg obj
            conv.i
            ret
#endif
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T PtrToObject<T>(IntPtr obj) where T : class
        {
#if IL
            object convObj; // TEMP XAMARIN AOT FIX -- DOES NOT WORK FOR VALUE TYPE PROPERTIES
            ldarg obj
            stloc convObj // TEMP XAMARIN AOT FIX -- DOES NOT WORK FOR VALUE TYPE PROPERTIES
            ldloc convObj // TEMP XAMARIN AOT FIX -- DOES NOT WORK FOR VALUE TYPE PROPERTIES
            ret
#endif
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Needed for Xamarin AOT
        public static IntPtr Unbox<T>(object obj)
        {
#if IL
            ldarg obj
            unbox !!T
            ret
#endif
            throw new NotImplementedException();
        }

        private static int ComputeArrayFirstElementOffset()
        {
            var testArray = new int[1];
            fixed (int* testArrayStart = testArray)
            {
                var testArrayObjectStart = ObjectToPtr(testArray);
                return (int)((byte*)testArrayStart - (byte*)testArrayObjectStart);
            }
        }
    }
}
