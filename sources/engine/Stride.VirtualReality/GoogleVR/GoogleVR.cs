// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if DONT_BUILD_FOR_NOW && (STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_ANDROID)

using System;
using System.Runtime.InteropServices;
using System.Security;
using OpenTK.Graphics.ES20;
using Stride.Core.Mathematics;
using Stride.Graphics;

#if STRIDE_PLATFORM_ANDROID
using Java.Lang;
using Android.App;
using Android.Views;
using Com.Google.VR.Ndk.Base;
using Stride.Games;
#endif

namespace Stride.VirtualReality
{
    public static class GoogleVr
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrStartup", CallingConvention = CallingConvention.Cdecl)]
        private static extern int InternalStartup(IntPtr ctx);

#if STRIDE_PLATFORM_ANDROID

        public static void Startup(Game game, Activity androidActivity)
        {
            game.WindowCreated += (sender1, args1) =>
            {
                var androidWindow = (GameWindowAndroid)game.Window;

                // Setup VR layout
                var layout = new GvrLayout(androidActivity);
                if (layout.SetAsyncReprojectionEnabled(true))
                {
                    AndroidCompat.SetSustainedPerformanceMode(androidActivity, true);
                }
                AndroidCompat.SetVrModeEnabled(androidActivity, true);

                ((ViewGroup)androidWindow.StrideGameForm.Parent).RemoveView(androidWindow.StrideGameForm);
                layout.SetPresentationView(androidWindow.StrideGameForm);
                androidActivity.SetContentView(layout);

                // Init native, we need to reflect some methods that xamarin failed to wrap
                var classGvrLayout = Class.ForName("com.google.vr.ndk.base.GvrLayout");
                var getGvrApiMethod = classGvrLayout.GetMethod("getGvrApi");
                var gvrApi = getGvrApiMethod.Invoke(layout);
                var classGvrApi = Class.ForName("com.google.vr.ndk.base.GvrApi");
                var getNativeGvrContextMethod = classGvrApi.GetMethod("getNativeGvrContext");
                var nativeContextLong = (long)getNativeGvrContextMethod.Invoke(gvrApi);
                var nativeCtx = new IntPtr(nativeContextLong);

                if (InternalStartup(nativeCtx) != 0)
                {
                    throw new System.Exception("Failed to Startup Google VR SDK");
                }
            };
        }

#elif STRIDE_PLATFORM_IOS

        public static void Startup(Game game)
        {
            game.WindowCreated += (sender1, args1) =>
            {
                var res = InternalStartup(IntPtr.Zero);
                if (res != 0)
                {
                    throw new Exception("Failed to init Google cardboad SDK.");
                }
            };
        }

#endif

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrGetMaxRenderSize", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetMaxRenderSize(out int width, out int height);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrInit", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Init(int width, int height);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrGetPerspectiveMatrix", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetPerspectiveMatrix(int eyeIndex, float near, float far, out Matrix headMatrix);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrGetHeadMatrix", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetHeadMatrix(out Matrix headMatrix);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrGetEyeMatrix", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetEyeMatrix(int eyeIndex, out Matrix headMatrix);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrGetNextFrame", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetNextFrame();

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrGetFBOIndex", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetFBOIndex(IntPtr frame, int index);

        public static void SubmitRenderTarget(GraphicsContext context, Texture renderTarget, IntPtr vrFrame, int index)
        {
            int currentFrameBuffer;
            GL.GetInteger(GetPName.FramebufferBinding, out currentFrameBuffer);

            //TODO add proper destination values
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GetFBOIndex(vrFrame, index));
            // The next call will write straight to the previously bound buffer
            context.CommandList.CopyScaler2D(renderTarget,
                new Rectangle(0, 0, renderTarget.Width, renderTarget.Height),
                new Rectangle(0, 0, renderTarget.Width, renderTarget.Height)
            );

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, currentFrameBuffer);
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport(NativeInvoke.Library, EntryPoint = "xnGvrSubmitFrame", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InternalSubmitFrame(IntPtr frame, ref Matrix headMatrix);

        public static bool SubmitFrame(GraphicsDevice graphicsDevice, IntPtr frame, ref Matrix headMatrix)
        {
            int currentFrameBuffer;
            GL.GetInteger(GetPName.FramebufferBinding, out currentFrameBuffer);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, graphicsDevice.FindOrCreateFBO(graphicsDevice.Presenter.BackBuffer));

            var res = InternalSubmitFrame(frame, ref headMatrix);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, currentFrameBuffer);

            return res;
        }
    }
}

#endif
