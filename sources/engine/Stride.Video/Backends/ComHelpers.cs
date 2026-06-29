// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
#pragma warning disable CA1416 // Validate platform compatibility (Windows-only)

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32;

/// <summary>CsWin32 supporting infrastructure used by the MediaEngine video backend.</summary>
static unsafe partial class ComHelpers
{
    public static MFComPtr<T> CreateCCW<T>(object instance) where T : unmanaged, IVTable, IComIID
    {
        var unknown = (IUnknown*)MFComWrappers<T>.Instance.GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.None);
        unknown->QueryInterface<T>(out var ppv);
        unknown->Release();
        return new MFComPtr<T>(ppv);
    }

    public static unsafe MFComPtr<T> AsPtr<T>(T* ptr) where T : unmanaged, IComIID => new MFComPtr<T>(ptr);

    public static BSTRPtr AsBSTR(string str)
    {
        var bstr = Marshal.StringToBSTR(str);
        return new BSTRPtr(bstr);
    }

    // Called by CsWin32
    static partial void PopulateIUnknownImpl<TComInterface>(IUnknown.Vtbl* vtable)
        where TComInterface : unmanaged
    {
        ComWrappers.GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);
        vtable->QueryInterface_1 = (delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, HRESULT>)fpQueryInterface;
        vtable->AddRef_2 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpAddRef;
        vtable->Release_3 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpRelease;
    }

    // https://github.com/microsoft/CsWin32/issues/751#issuecomment-1304268295
    private sealed class MFComWrappers<T> : ComWrappers
        where T : IVTable, IComIID
    {
        public static readonly ComWrappers Instance = new MFComWrappers<T>();

        private static readonly ComInterfaceEntry* s_comInterfaceEntries = CreateComInterfaceEntries();

        private static ComInterfaceEntry* CreateComInterfaceEntries()
        {
            var comInterfaceEntries = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(T), sizeof(ComInterfaceEntry));
            comInterfaceEntries->IID = T.Guid;
            comInterfaceEntries->Vtable = new IntPtr(T.VTable);
            return comInterfaceEntries;
        }

        protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            count = 1;
            return s_comInterfaceEntries;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags) => throw new NotImplementedException();
        protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
    }

    /// <summary>Ensures Release() is called on the wrapped COM pointer.</summary>
    public ref struct MFComPtr<T> : IDisposable where T : unmanaged, IComIID
    {
        public readonly T* Ptr;
        public MFComPtr(T* ptr) => Ptr = ptr;
        public void Dispose() => ((IUnknown*)Ptr)->Release();
        public static implicit operator T*(in MFComPtr<T> comPtr) => comPtr.Ptr;
        public static implicit operator IUnknown*(in MFComPtr<T> comPtr) => (IUnknown*)comPtr.Ptr;
        public static explicit operator MFComPtr<T>(T* ptr) => new MFComPtr<T>(ptr);
    }

    /// <summary>Ensures FreeBSTR is called on the wrapped BSTR.</summary>
    public ref struct BSTRPtr : IDisposable
    {
        public readonly nint Ptr;
        public BSTRPtr(nint ptr) => Ptr = ptr;
        public void Dispose() => Marshal.FreeBSTR(Ptr);
        public static implicit operator BSTR(in BSTRPtr bstrPtr) => new BSTR(bstrPtr.Ptr);
    }
}

#endif
