// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
#pragma warning disable SA1405 // Debug.Assert must provide message text
using System;
using System.Diagnostics;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract partial class GraphicsResourceBase
    {
        // TODO : Needs review
        private ID3D11Resource nativeResource;
        private ID3D11DeviceChild nativeDeviceChild;

        protected internal ID3D11Resource NativeResource { get; set; }

        private void Initialize()
        {
        }

        /// <summary>
        /// Gets or sets the device child.
        /// </summary>
        /// <value>The device child.</value>
        protected internal ID3D11DeviceChild NativeDeviceChild
        {
            get
            {
                return nativeDeviceChild;
            }
            set
            {
                nativeDeviceChild = value;

                unsafe
                {
                    fixed(ID3D11DeviceChild* child = &nativeDeviceChild)
                    {
                        ID3D11Resource* res = null;
                        SilkMarshal.ThrowHResult(child->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&res));
                        nativeResource = *res;
                    }
                }
                // Todo : Debug name ? 
                //SetDebugName(GraphicsDevice, nativeDeviceChild, Name);
            }
        }

        /// <summary>
        /// Associates the private data to the device child, useful to get the name in PIX debugger.
        /// </summary>
        internal static void SetDebugName(GraphicsDevice graphicsDevice, ID3D11DeviceChild deviceChild, string name)
        {
            //deviceChild.Name = name;
        }

        /// <summary>
        /// Called when graphics device has been detected to be internally destroyed.
        /// </summary>
        protected internal virtual void OnDestroyed()
        {
            Destroyed?.Invoke(this, EventArgs.Empty);

            nativeDeviceChild.Release();
            NativeResource.Release();
        }

        /// <summary>
        /// Called when graphics device has been recreated.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Active"/> state.</returns>
        protected internal virtual bool OnRecreate()
        {
            return false;
        }

        protected ID3D11Device NativeDevice
        {
            get
            {
                //TODO : check if this is correct
                return GraphicsDevice != null ? GraphicsDevice.NativeDevice : new ID3D11Device();
            }
        }

        /// <summary>
        /// Gets the cpu access flags from resource usage.
        /// </summary>
        /// <param name="usage">The usage.</param>
        /// <returns></returns>
        internal static CpuAccessFlag GetCpuAccessFlagsFromUsage(GraphicsResourceUsage usage)
        {
            switch (usage)
            {
                case GraphicsResourceUsage.Dynamic:
                    return CpuAccessFlag.CpuAccessWrite;
                case GraphicsResourceUsage.Staging:
                    return CpuAccessFlag.CpuAccessRead | CpuAccessFlag.CpuAccessWrite;
                default:
                    return CpuAccessFlag.CpuAccessRead;
            }
        }

    }
}
 
#endif
