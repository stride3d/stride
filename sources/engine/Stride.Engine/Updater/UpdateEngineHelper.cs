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
            #if false
            ldarga.s address
            conv.u
            nop         // call !!0& Unsafe::AsRef(void*) /* ldarg.0; ret */
            ldobj !!T
            ret
            #endif
            => Unsafe.AsRef<T>(&address);
        /// <summary>Copies the value out of the pinned box.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("Do not use.", DiagnosticId = "STRIDE2000")]
        public static unsafe T PointerToStruct<T>(nint address) where T : struct
            #if false
            ldarga.s address
            conv.u
            nop         // call !!0& Unsafe::AsRef<object>(void*) /* ldarg.0; ret */
            ldind.ref
            unbox.any !!T
            ret
            #endif
            => (T)Unsafe.AsRef<object>(&address);
        /// <summary>Obtains a reference to the object at the specified address,
        /// then unboxes the value of type <typeparamref name="T"/> and
        /// returns the controlled-mutability managed pointer.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("Do not use.", DiagnosticId = "STRIDE2000")]
        public static unsafe ref T RefBoxedStruct<T>(nint address) where T : struct
            #if false
            ldarga.s address
            conv.u
            nop         // call !!0& Unsafe::AsRef<object>(void*) /* ldarg.0; ret */
            ldobj System.Object
            unbox !!T
            ret
            #endif
            => ref Unsafe.Unbox<T>(PointerToObject<object>(address));

        [Obsolete("Use ObjectToPointer instead.")]
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

        [Obsolete("Use PointerToObject instead.")]
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

        [Obsolete("Use Unsafe.Unbox instead.")]
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
                var testArrayObjectStart = ObjectToPointer(testArray);
                return (int)((byte*)testArrayStart - (byte*)testArrayObjectStart);
            }
        }
    }
}
