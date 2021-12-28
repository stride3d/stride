// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        private const string LibraryName = "renderdoc.dll";

        private bool isCaptureStarted;
        private unsafe IntPtr* apiPointers;

        public unsafe bool IsInitialized
        {
            get { return apiPointers != null; }
        }

        // Matching https://github.com/baldurk/renderdoc/blob/master/renderdoc/api/app/renderdoc_app.h

        public unsafe RenderDocManager()
        {
            var reg = Registry.ClassesRoot.OpenSubKey("CLSID\\" + RenderdocClsid + "\\InprocServer32");
            if (reg == null)
            {
                return;
            }
            var path = reg.GetValue(null) != null ? reg.GetValue(null).ToString() : null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            // Preload the library before using the UnmanagedFunctionPointerAttribute
            var ptr = LoadLibrary(path);
            if (ptr == IntPtr.Zero)
            {
                return;
            }

            var getAPIAddress = GetProcAddress(ptr, nameof(RENDERDOC_GetAPI));
            if (getAPIAddress == IntPtr.Zero)
                return;

            // Get main entry point to get other function pointers
            var getAPI = Marshal.GetDelegateForFunctionPointer<RENDERDOC_GetAPI>(getAPIAddress);

            // API version 10400 has 25 function pointers
            if (!getAPI(RENDERDOC_API_VERSION, ref apiPointers))
                return;
        }

        public unsafe void Initialize(string captureFilePath = null)
        {
            var finalLogFilePath = captureFilePath ?? FindAvailablePath("RenderDoc" + Assembly.GetEntryAssembly().Location);
            GetMethod<RENDERDOC_SetCaptureFilePathTemplate>(RenderDocAPIFunction.SetCaptureFilePathTemplate)(finalLogFilePath);

            var focusToggleKey = KeyButton.eRENDERDOC_Key_F11;
            GetMethod<RENDERDOC_SetFocusToggleKeys>(RenderDocAPIFunction.SetFocusToggleKeys)(ref focusToggleKey, 1);
            var captureKey = KeyButton.eRENDERDOC_Key_F12;
            GetMethod<RENDERDOC_SetCaptureKeys>(RenderDocAPIFunction.SetCaptureKeys)(ref captureKey, 1);
        }

        public void RemoveHooks()
        {
            if (IsInitialized)
            {
                GetMethod<RENDERDOC_RemoveHooks>(RenderDocAPIFunction.RemoveHooks)();
            }
        }

        public void StartFrameCapture(GraphicsDevice graphicsDevice, IntPtr hwndPtr)
        {
            GetMethod<RENDERDOC_StartFrameCapture>(RenderDocAPIFunction.StartFrameCapture)(GetDevicePointer(graphicsDevice), hwndPtr);
            isCaptureStarted = true;
        }

        public void EndFrameCapture(GraphicsDevice graphicsDevice, IntPtr hwndPtr)
        {
            if (!isCaptureStarted)
                return;

            GetMethod<RENDERDOC_EndFrameCapture>(RenderDocAPIFunction.EndFrameCapture)(GetDevicePointer(graphicsDevice), hwndPtr);
            isCaptureStarted = false;
        }

        public void DiscardFrameCapture(GraphicsDevice graphicsDevice, IntPtr hwndPtr)
        {
            if (!isCaptureStarted)
                return;

            GetMethod<RENDERDOC_DiscardFrameCapture>(RenderDocAPIFunction.DiscardFrameCapture)(GetDevicePointer(graphicsDevice), hwndPtr);
            isCaptureStarted = false;
        }

        private static IntPtr GetDevicePointer(GraphicsDevice graphicsDevice)
        {
            var devicePointer = IntPtr.Zero;
#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_DIRECT3D12
            if (graphicsDevice != null)
                devicePointer = ((SharpDX.CppObject)SharpDXInterop.GetNativeDevice(graphicsDevice)).NativePointer;
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
                {
                    path += i;
                }
                path += ".rdc";

                if (!File.Exists(path))
                {
                    return Path.Combine(Path.GetDirectoryName(logFilePath), path);
                }
            }
            return logFilePath;
        }

        private enum KeyButton : uint
        {
            // '0' - '9' matches ASCII values
            eRENDERDOC_Key_0 = 0x30,
            eRENDERDOC_Key_1 = 0x31,
            eRENDERDOC_Key_2 = 0x32,
            eRENDERDOC_Key_3 = 0x33,
            eRENDERDOC_Key_4 = 0x34,
            eRENDERDOC_Key_5 = 0x35,
            eRENDERDOC_Key_6 = 0x36,
            eRENDERDOC_Key_7 = 0x37,
            eRENDERDOC_Key_8 = 0x38,
            eRENDERDOC_Key_9 = 0x39,

            // 'A' - 'Z' matches ASCII values
            eRENDERDOC_Key_A = 0x41,
            eRENDERDOC_Key_B = 0x42,
            eRENDERDOC_Key_C = 0x43,
            eRENDERDOC_Key_D = 0x44,
            eRENDERDOC_Key_E = 0x45,
            eRENDERDOC_Key_F = 0x46,
            eRENDERDOC_Key_G = 0x47,
            eRENDERDOC_Key_H = 0x48,
            eRENDERDOC_Key_I = 0x49,
            eRENDERDOC_Key_J = 0x4A,
            eRENDERDOC_Key_K = 0x4B,
            eRENDERDOC_Key_L = 0x4C,
            eRENDERDOC_Key_M = 0x4D,
            eRENDERDOC_Key_N = 0x4E,
            eRENDERDOC_Key_O = 0x4F,
            eRENDERDOC_Key_P = 0x50,
            eRENDERDOC_Key_Q = 0x51,
            eRENDERDOC_Key_R = 0x52,
            eRENDERDOC_Key_S = 0x53,
            eRENDERDOC_Key_T = 0x54,
            eRENDERDOC_Key_U = 0x55,
            eRENDERDOC_Key_V = 0x56,
            eRENDERDOC_Key_W = 0x57,
            eRENDERDOC_Key_X = 0x58,
            eRENDERDOC_Key_Y = 0x59,
            eRENDERDOC_Key_Z = 0x5A,

            // leave the rest of the ASCII range free
            // in case we want to use it later
            eRENDERDOC_Key_NonPrintable = 0x100,

            eRENDERDOC_Key_Divide,
            eRENDERDOC_Key_Multiply,
            eRENDERDOC_Key_Subtract,
            eRENDERDOC_Key_Plus,

            eRENDERDOC_Key_F1,
            eRENDERDOC_Key_F2,
            eRENDERDOC_Key_F3,
            eRENDERDOC_Key_F4,
            eRENDERDOC_Key_F5,
            eRENDERDOC_Key_F6,
            eRENDERDOC_Key_F7,
            eRENDERDOC_Key_F8,
            eRENDERDOC_Key_F9,
            eRENDERDOC_Key_F10,
            eRENDERDOC_Key_F11,
            eRENDERDOC_Key_F12,

            eRENDERDOC_Key_Home,
            eRENDERDOC_Key_End,
            eRENDERDOC_Key_Insert,
            eRENDERDOC_Key_Delete,
            eRENDERDOC_Key_PageUp,
            eRENDERDOC_Key_PageDn,

            eRENDERDOC_Key_Backspace,
            eRENDERDOC_Key_Tab,
            eRENDERDOC_Key_PrtScrn,
            eRENDERDOC_Key_Pause,

            eRENDERDOC_Key_Max,
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
