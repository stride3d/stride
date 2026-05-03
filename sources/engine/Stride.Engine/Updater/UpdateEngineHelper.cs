// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable STRIDE2000 // TODO: Remove this suppression
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Stride.Updater
{
    /// <summary>
    /// Various helper functions for the <see cref="UpdateEngine"/>.
    /// </summary>
    internal static unsafe class UpdateEngineHelper
    {
        public static int ArrayFirstElementOffset = ComputeArrayFirstElementOffset();

        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("Do not use.", DiagnosticId = "STRIDE2000")]
        public static unsafe nint ObjectToPointer(object o)
            #if false
            ldarga.s o
            conv.u      // call void* Unsafe::AsPointer(!!0&) /* ldarg.0; conv.u; ret */
            ldind.i
            ret
            #endif
            => ((nint*)Unsafe.AsPointer(ref o))[0];
        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("Do not use.", DiagnosticId = "STRIDE2000")]
        public static unsafe T PointerToObject<T>(nint address) where T : class
            => Unsafe.AsRef<T>(&address);
        /// <summary>Copies the value out of the pinned box.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("Do not use.", DiagnosticId = "STRIDE2000")]
        public static unsafe T PointerToStruct<T>(nint address) where T : struct
            => (T)Unsafe.AsRef<object>(&address);
        /// <summary>Obtains a reference to the object at the specified address,
        /// then unboxes the value of type <typeparamref name="T"/> and
        /// returns the controlled-mutability managed pointer.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("Do not use.", DiagnosticId = "STRIDE2000")]
        public static unsafe ref T RefBoxedStruct<T>(nint address) where T : struct
            => ref Unsafe.Unbox<T>(PointerToObject<object>(address));

        private static int ComputeArrayFirstElementOffset()
        {
            var testArray = new int[1];
            fixed (int* testArrayStart = testArray)
            {
                var testArrayObjectStart = ObjectToPointer(testArray);
                return (int)((byte*)testArrayStart - (byte*)testArrayObjectStart);
            }
        }
    }
}
