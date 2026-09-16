// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Stride.GameStudio;

/// <summary>
/// Taskbar identity of this install: every window gets the shell's relaunch properties (ID, display name, icon,
/// command) and, when it has no icon of its own, the apphost's icon. Needed when the editor is re-executed under
/// the generic <c>dotnet.exe</c> host, which the taskbar would otherwise show as ".NET Host" with its own icon;
/// applied under the apphost too so both hosts of one install share a taskbar group and a pin.
/// </summary>
internal static class HostedWindowIdentity
{
    private static string appId, displayName, relaunchExe;
    private static BitmapSource icon;
    private static readonly HashSet<IntPtr> stamped = [];

    public static void Install(string id, string name, string exePath)
    {
        appId = id;
        displayName = name;
        relaunchExe = System.IO.File.Exists(exePath) ? exePath : null;
        SetCurrentProcessExplicitAppUserModelID(id);
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        var window = (Window)sender;
        try
        {
            if (window.Icon == null && relaunchExe != null)
                window.Icon = icon ??= LoadIcon(relaunchExe);
            // Loaded fires again when a window is shown again; the handle is stamped once.
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero && stamped.Add(hwnd))
                ApplyRelaunchProperties(hwnd);
        }
        catch
        {
            // Cosmetic: the window works without a taskbar identity.
        }
    }

    private static BitmapSource LoadIcon(string exePath)
    {
        using var extracted = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        if (extracted == null)
            return null;
        var source = Imaging.CreateBitmapSourceFromHIcon(extracted.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        source.Freeze();
        return source;
    }

    private static void ApplyRelaunchProperties(IntPtr hwnd)
    {
        var iid = typeof(IPropertyStore).GUID;
        if (SHGetPropertyStoreForWindow(hwnd, ref iid, out var store) != 0 || store == null)
            return;
        try
        {
            SetString(store, AppUserModelId, appId);
            SetString(store, AppUserModelRelaunchDisplayNameResource, displayName);
            if (relaunchExe != null)
            {
                SetString(store, AppUserModelRelaunchCommand, $"\"{relaunchExe}\"");
                SetString(store, AppUserModelRelaunchIconResource, $"{relaunchExe},0");
            }
            store.Commit();
        }
        finally
        {
            Marshal.ReleaseComObject(store);
        }
    }

    private static void SetString(IPropertyStore store, PropertyKey key, string value)
    {
        var variant = new PropVariant { vt = VT_LPWSTR, pointerValue = Marshal.StringToCoTaskMemUni(value) };
        try
        {
            store.SetValue(ref key, ref variant);
        }
        finally
        {
            Marshal.FreeCoTaskMem(variant.pointerValue);
        }
    }

    // System.AppUserModel.* property keys (propkey.h).
    private static readonly Guid AppUserModelFormat = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    private static readonly PropertyKey AppUserModelRelaunchCommand = new(AppUserModelFormat, 2);
    private static readonly PropertyKey AppUserModelRelaunchIconResource = new(AppUserModelFormat, 3);
    private static readonly PropertyKey AppUserModelRelaunchDisplayNameResource = new(AppUserModelFormat, 4);
    private static readonly PropertyKey AppUserModelId = new(AppUserModelFormat, 5);
    private const ushort VT_LPWSTR = 31;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PropertyKey(Guid formatId, uint propertyId)
    {
        public Guid FormatId = formatId;
        public uint PropertyId = propertyId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort vt;
        public ushort reserved1, reserved2, reserved3;
        public IntPtr pointerValue;
        public IntPtr pointerValue2;
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint count);
        void GetAt(uint index, out PropertyKey key);
        void GetValue(ref PropertyKey key, out PropVariant value);
        void SetValue(ref PropertyKey key, ref PropVariant value);
        void Commit();
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore store);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
