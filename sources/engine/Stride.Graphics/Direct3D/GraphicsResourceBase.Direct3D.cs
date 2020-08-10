// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
#pragma warning disable SA1405 // Debug.Assert must provide message text
using System;
using System.Diagnostics;

using SharpDX;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract partial class GraphicsResourceBase
    {
        private SharpDX.Direct3D11.DeviceChild nativeDeviceChild;

        protected internal SharpDX.Direct3D11.Resource NativeResource { get; private set; }

        private void Initialize()
        {
        }

        /// <summary>
        /// Gets or sets the device child.
        /// </summary>
        /// <value>The device child.</value>
        protected internal SharpDX.Direct3D11.DeviceChild NativeDeviceChild
        {
            get
            {
                return nativeDeviceChild;
            }
            set
            {
                nativeDeviceChild = value;
                NativeResource = nativeDeviceChild as SharpDX.Direct3D11.Resource;
                // Associate PrivateData to this DeviceResource
                SetDebugName(GraphicsDevice, nativeDeviceChild, Name);
            }
        }

        /// <summary>
        /// Associates the private data to the device child, useful to get the name in PIX debugger.
        /// </summary>
        internal static void SetDebugName(GraphicsDevice graphicsDevice, SharpDX.Direct3D11.DeviceChild deviceChild, string name)
        {
            if (graphicsDevice.IsDebugMode && deviceChild != null)
            {
                deviceChild.DebugName = name;
            }
        }

        /// <summary>
        /// Called when graphics device has been detected to be internally destroyed.
        /// </summary>
        protected internal virtual void OnDestroyed()
        {
            Destroyed?.Invoke(this, EventArgs.Empty);

            ReleaseComObject(ref nativeDeviceChild);
            NativeResource = null;
        }

        /// <summary>
        /// Called when graphics device has been recreated.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Active"/> state.</returns>
        protected internal virtual bool OnRecreate()
        {
            return false;
        }

        protected SharpDX.Direct3D11.Device NativeDevice
        {
            get
            {
                return GraphicsDevice != null ? GraphicsDevice.NativeDevice : null;
            }
        }

        /// <summary>
        /// Gets the cpu access flags from resource usage.
        /// </summary>
        /// <param name="usage">The usage.</param>
        /// <returns></returns>
        internal static SharpDX.Direct3D11.CpuAccessFlags GetCpuAccessFlagsFromUsage(GraphicsResourceUsage usage)
        {
            switch (usage)
            {
                case GraphicsResourceUsage.Dynamic:
                    return SharpDX.Direct3D11.CpuAccessFlags.Write;
                case GraphicsResourceUsage.Staging:
                    return SharpDX.Direct3D11.CpuAccessFlags.Read | SharpDX.Direct3D11.CpuAccessFlags.Write;
            }
            return SharpDX.Direct3D11.CpuAccessFlags.None;
        }

        internal static void ReleaseComObject<T>(ref T comObject) where T : class
        {
            // We can't put IUnknown as a constraint on the generic as it would break compilation (trying to import SharpDX in projects with InternalVisibleTo)
            var iUnknownObject = comObject as IUnknown;
            if (iUnknownObject != null)
            {
                var refCountResult = iUnknownObject.Release();
                Debug.Assert(refCountResult >= 0);
                comObject = null;
            }
        }
    }
}
 
#endif
