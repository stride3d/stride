// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace Stride.Graphics
{
    public class RenderDocManager
    {
        private const string RenderdocClsid = "{5D6BF029-A6BA-417A-8523-120492B1DCE3}";

        private static unsafe IntPtr* sharedApiPointers;
        // Guard against concurrent captures — RenderDoc currently only supports one active device at a time.
        // This can be removed once RenderDoc supports multiple simultaneous device captures.
        private static GraphicsDevice activeCaptureDevice;

        private bool isCaptureStarted;
        private unsafe IntPtr* apiPointers;

        public unsafe bool IsInitialized => apiPointers != null;

        // Matching https://github.com/baldurk/renderdoc/blob/master/renderdoc/api/app/renderdoc_app.h

        public unsafe RenderDocManager()
        {
            // Only load the RenderDoc library once
            if (sharedApiPointers != null)
            {
                apiPointers = sharedApiPointers;
                return;
            }

            // Allow overriding RenderDoc DLL path via environment variable
            var path = Environment.GetEnvironmentVariable("STRIDE_RENDERDOC_LIBRARY_PATH");

            if (string.IsNullOrEmpty(path))
            {
                var reg = Registry.ClassesRoot.OpenSubKey("CLSID\\" + RenderdocClsid + "\\InprocServer32");
                if (reg == null)
                    return;

                path = reg.GetValue(null)?.ToString();
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            var ptr = LoadLibrary(path);
            if (ptr == IntPtr.Zero)
                return;

            var getAPIAddress = GetProcAddress(ptr, nameof(RENDERDOC_GetAPI));
            if (getAPIAddress == IntPtr.Zero)
                return;

            var getAPI = Marshal.GetDelegateForFunctionPointer<RENDERDOC_GetAPI>(getAPIAddress);

            if (!getAPI(RENDERDOC_API_VERSION, ref sharedApiPointers))
                return;

            apiPointers = sharedApiPointers;

            // Allow debug/validation output even when RenderDoc is attached
            GetMethod<RENDERDOC_SetCaptureOptionU32>(RenderDocAPIFunction.SetCaptureOptionU32)(CaptureOption.DebugOutputMute, 0);
        }

        public void Initialize(string captureFilePath = null)
        {
            var finalLogFilePath = captureFilePath ?? FindAvailablePath(Assembly.GetEntryAssembly().Location);
            GetMethod<RENDERDOC_SetCaptureFilePathTemplate>(RenderDocAPIFunction.SetCaptureFilePathTemplate)(finalLogFilePath);

            var focusToggleKey = KeyButton.eRENDERDOC_Key_F11;
            GetMethod<RENDERDOC_SetFocusToggleKeys>(RenderDocAPIFunction.SetFocusToggleKeys)(ref focusToggleKey, 1);
            var captureKey = KeyButton.eRENDERDOC_Key_F12;
            GetMethod<RENDERDOC_SetCaptureKeys>(RenderDocAPIFunction.SetCaptureKeys)(ref captureKey, 1);
        }

        public void RemoveHooks()
        {
            if (IsInitialized)
                GetMethod<RENDERDOC_RemoveHooks>(RenderDocAPIFunction.RemoveHooks)();
        }

        public void StartFrameCapture(GraphicsDevice graphicsDevice, IntPtr hwndPtr)
        {
            if (activeCaptureDevice is not null)
                throw new InvalidOperationException("A RenderDoc capture is already in progress. End or discard the current capture before starting a new one.");

            activeCaptureDevice = graphicsDevice;
            GetMethod<RENDERDOC_StartFrameCapture>(RenderDocAPIFunction.StartFrameCapture)(GetDevicePointer(graphicsDevice), hwndPtr);
            isCaptureStarted = true;
        }

        public void EndFrameCapture(GraphicsDevice graphicsDevice, IntPtr hwndPtr)
        {
            if (!isCaptureStarted)
                return;

            if (graphicsDevice != activeCaptureDevice)
                throw new InvalidOperationException("RenderDoc EndFrameCapture called with a different device than StartFrameCapture.");

            GetMethod<RENDERDOC_EndFrameCapture>(RenderDocAPIFunction.EndFrameCapture)(GetDevicePointer(graphicsDevice), hwndPtr);
            isCaptureStarted = false;
            activeCaptureDevice = null;
        }

        public void DiscardFrameCapture(GraphicsDevice graphicsDevice, IntPtr hwndPtr)
        {
            if (!isCaptureStarted)
                return;

            if (graphicsDevice != activeCaptureDevice)
                throw new InvalidOperationException("RenderDoc DiscardFrameCapture called with a different device than StartFrameCapture.");

            GetMethod<RENDERDOC_DiscardFrameCapture>(RenderDocAPIFunction.DiscardFrameCapture)(GetDevicePointer(graphicsDevice), hwndPtr);
            isCaptureStarted = false;
            activeCaptureDevice = null;
        }

        private static unsafe nint GetDevicePointer(GraphicsDevice graphicsDevice)
        {
            nint devicePointer = 0;

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_DIRECT3D12
            if (graphicsDevice is not null)
                devicePointer = (nint) GraphicsMarshal.GetNativeDevice(graphicsDevice).Handle;
#elif STRIDE_GRAPHICS_API_VULKAN
            // RenderDoc Vulkan capture uses NULL (wildcard) for the device pointer.
            // Passing VkInstance.Handle crashes with some ICDs (e.g. SwiftShader).
#endif
            return devicePointer;
        }

        private unsafe TDelegate GetMethod<TDelegate>(RenderDocAPIFunction function)
        {
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(apiPointers[(int)function]);
        }

        private static string FindAvailablePath(string logFilePath)
        {
            var filePath = Path.GetFileNameWithoutExtension(logFilePath);
            for (int i = 0; i < 1000000; i++)
            {
                var path = filePath;
                if (i > 0)
                    path += i;
                path += ".rdc";

                if (!File.Exists(path))
                    return Path.Combine(Path.GetDirectoryName(logFilePath), path);
            }
            return logFilePath;
        }

        private enum KeyButton : uint
        {
            eRENDERDOC_Key_F11 = 0x100 + 14,
            eRENDERDOC_Key_F12 = 0x100 + 15,
        };

        [Flags]
        private enum InAppOverlay : uint
        {
            eOverlay_Enabled = 0x1,
            eOverlay_FrameRate = 0x2,
            eOverlay_FrameNumber = 0x4,
            eOverlay_CaptureList = 0x8,
            eOverlay_Default = (eOverlay_Enabled | eOverlay_FrameRate | eOverlay_FrameNumber | eOverlay_CaptureList),
            eOverlay_All = 0xFFFFFFFF,
            eOverlay_None = 0,
        };

        // API breaking change history:
        // Version 1 -> 2 - strings changed from wchar_t* to char* (UTF-8)
        private const int RENDERDOC_API_VERSION = 10400;

        private enum CaptureOption : uint
        {
            AllowVSync = 0,
            AllowFullscreen = 1,
            APIValidation = 2,
            CaptureCallstacks = 3,
            CaptureCallstacksOnlyDraws = 4,
            DelayForDebugger = 5,
            VerifyBufferAccess = 6,
            HookIntoChildren = 7,
            RefAllResources = 8,
            SaveAllInitials = 9,
            CaptureAllCmdLists = 10,
            DebugOutputMute = 11,
            AllowUnsupportedVendorExtensions = 12,
            SoftMemoryLimit = 13,
        }

        private enum RenderDocAPIFunction
        {
            GetAPIVersion,

            SetCaptureOptionU32,
            SetCaptureOptionF32,

            GetCaptureOptionU32,
            GetCaptureOptionF32,

            SetFocusToggleKeys,
            SetCaptureKeys,

            GetOverlayBits,
            MaskOverlayBits,

            RemoveHooks,
            UnloadCrashHandler,

            SetCaptureFilePathTemplate,
            GetCaptureFilePathTemplate,

            GetNumCaptures,
            GetCapture,

            TriggerCapture,

            IsTargetControlConnected,
            LaunchReplayUI,

            SetActiveWindow,

            StartFrameCapture,
            IsFrameCapturing,
            EndFrameCapture,

            TriggerMultiFrameCapture,
            SetCaptureFileComments,
            DiscardFrameCapture,
        }

        //////////////////////////////////////////////////////////////////////////
        // In-program functions
        //////////////////////////////////////////////////////////////////////////
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private unsafe delegate bool RENDERDOC_GetAPI(int version, ref IntPtr* apiPointers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate bool RENDERDOC_SetCaptureOptionU32(CaptureOption option, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_RemoveHooks();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_SetCaptureFilePathTemplate([MarshalAs(UnmanagedType.LPStr)] string logfile);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate string RENDERDOC_GetLogFilePathTemplate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate bool RENDERDOC_GetCapture(int idx, [MarshalAs(UnmanagedType.LPStr)] string logfile, out int pathlength, out long timestamp);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_SetActiveWindow(IntPtr devicePointer, IntPtr wndHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_TriggerCapture();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_StartFrameCapture(IntPtr devicePointer, IntPtr wndHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate bool RENDERDOC_EndFrameCapture(IntPtr devicePointer, IntPtr wndHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate InAppOverlay RENDERDOC_GetOverlayBits();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_MaskOverlayBits(InAppOverlay And, InAppOverlay Or);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_SetFocusToggleKeys(ref KeyButton keys, int num);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_SetCaptureKeys(ref KeyButton keys, int num);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate void RENDERDOC_UnloadCrashHandler();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate bool RENDERDOC_DiscardFrameCapture(IntPtr devicePointer, IntPtr wndHandle);

        [DllImport("kernel32", EntryPoint = "LoadLibrary", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    }
}
