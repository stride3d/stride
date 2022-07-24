// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
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

        /// <summary>Allocates a handle of the specified type for the specified object.
        /// <para>If the <paramref name="object"/> is of a reference type, it must have sequential or explicit layout.</para>
        /// <para>An instance with nonprimitive (non-blittable) members cannot be pinned.</para></summary>
        /// <returns>A new <see cref="GCHandle"/> of type <see cref="GCHandleType.Pinned"/>.
        /// This handle must be released with <see cref="GCHandle.Free"/> when it is no longer needed.</returns>
        public static GCHandle Pin<T>(T @object) where T : class {
            #if CHECKED
            static Func<bool> IsReferenceOrContainsReferences(Type type)
            {
                var method = pinMethod?.MakeGenericMethod(type)
                    ?? throw new MethodAccessException();
                return Expression.Lambda<Func<bool>>(Expression.Call(method)).Compile();
            }
            static V KeepExisting<K, V>(K key, V existing) => existing;
            var type = @object.GetType();
            if (!isPinnable.TryGetValue(type, out var func))
                func = isPinnable.AddOrUpdate(type, IsReferenceOrContainsReferences(type), KeepExisting);
            if (func() && !isPinnableBroken.ContainsKey(type))
            {
                isPinnableBroken.AddOrUpdate(type, true, KeepExisting);
            }
            #endif
            return GCHandle.Alloc(@object, GCHandleType.Pinned);
        }
        #if CHECKED
        private static readonly MethodInfo pinMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences), BindingFlags.Static | BindingFlags.Public);
        private static readonly ConcurrentDictionary<Type, Func<bool>> isPinnable = new();
        private static readonly ConcurrentDictionary<Type, bool> isPinnableBroken = new();
        #endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe nint ObjectToPointer(object o)
            #if false
            ldarga.s o
            conv.u      // call void* Unsafe::AsPointer(!!0&) /* ldarg.0; conv.u; ret */
            ldind.i
            ret
            #endif
            => ((nint*)Unsafe.AsPointer(ref o))[0];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
