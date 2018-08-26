// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_OPENGL
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Graphics;
#if XENKO_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Xenko.Graphics
{
    /// <summary>
    /// Used internally to provide a context for async resource creation
    /// (such as texture or buffer created on a thread where no context is active).
    /// </summary>
    internal struct UseOpenGLCreationContext : IDisposable
    {
        public readonly CommandList CommandList;

        private readonly bool useDeviceCreationContext;
        private readonly bool needUnbindContext;

        private readonly bool asyncCreationLockTaken;
        private readonly object asyncCreationLockObject;

        private readonly IGraphicsContext deviceCreationContext;

#if XENKO_PLATFORM_ANDROID
        private readonly bool tegraWorkaround;
#endif

#if XENKO_PLATFORM_IOS
        private OpenGLES.EAGLContext previousContext;
#endif

        public bool UseDeviceCreationContext => useDeviceCreationContext;

        public UseOpenGLCreationContext(GraphicsDevice graphicsDevice)
            : this()
        {
            if (OpenTK.Graphics.GraphicsContext.CurrentContextHandle.Handle == IntPtr.Zero)
            {
                needUnbindContext = true;
                useDeviceCreationContext = true;

#if XENKO_PLATFORM_ANDROID
                tegraWorkaround = graphicsDevice.Workaround_Context_Tegra2_Tegra3;

                // Notify main rendering thread there is some pending async work to do
                if (tegraWorkaround)
                {
                    useDeviceCreationContext = false; // We actually use real main context, so states will be kept
                    graphicsDevice.AsyncPendingTaskWaiting = true;
                }
#endif

                // Lock, since there is only one deviceCreationContext.
                // TODO: Support multiple deviceCreationContext (TLS creation of context was crashing, need to investigate why)
                asyncCreationLockObject = graphicsDevice.asyncCreationLockObject;
                Monitor.Enter(graphicsDevice.asyncCreationLockObject, ref asyncCreationLockTaken);

#if XENKO_PLATFORM_ANDROID
                if (tegraWorkaround)
                    graphicsDevice.AsyncPendingTaskWaiting = false;
#endif


#if XENKO_PLATFORM_IOS
                previousContext = OpenGLES.EAGLContext.CurrentContext;
                var localContext = graphicsDevice.ThreadLocalContext.Value;
                OpenGLES.EAGLContext.SetCurrentContext(localContext);
#else
                // Bind the context
                deviceCreationContext = graphicsDevice.deviceCreationContext;
                deviceCreationContext.MakeCurrent(graphicsDevice.deviceCreationWindowInfo);
#endif
            }
            else
            {
                // TODO Hardcoded to the fact it uses only one command list, this should be fixed
                CommandList = graphicsDevice.InternalMainCommandList;
            }
        }

        public void Dispose()
        {
            try
            {
                if (needUnbindContext)
                {
                    GL.Flush();

#if XENKO_PLATFORM_IOS
                    if (previousContext != null)
                        OpenGLES.EAGLContext.SetCurrentContext(previousContext);
#else
                    // Restore graphics context
                    GraphicsDevice.UnbindGraphicsContext(deviceCreationContext);
#endif
                }
            }
            finally
            {
                // Unlock
                if (asyncCreationLockTaken)
                {
#if XENKO_PLATFORM_ANDROID
                    if (tegraWorkaround)
                    {
                        // Notify GraphicsDevice.ExecutePendingTasks() that we are done.
                        Monitor.Pulse(asyncCreationLockObject);
                    }
#endif
                    Monitor.Exit(asyncCreationLockObject);
                }
            }
        }
    }
}
#endif
