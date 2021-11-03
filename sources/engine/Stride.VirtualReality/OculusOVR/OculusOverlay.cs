// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using Silk.NET.Direct3D11;
using Stride.Graphics;
using CommandList = Stride.Graphics.CommandList;

namespace Stride.VirtualReality
{
    internal class OculusOverlay : VROverlay, IDisposable
    {
        private readonly IntPtr ovrSession;
        internal IntPtr OverlayPtr;
        private readonly Texture[] textures;

        public OculusOverlay(IntPtr ovrSession, GraphicsDevice device, int width, int height, int mipLevels, int sampleCount)
        {
            int textureCount;
            this.ovrSession = ovrSession;
            unsafe
            {
                OverlayPtr = OculusOvr.CreateQuadLayerTexturesDx(ovrSession, (IntPtr)device.NativeDevice.Handle, out textureCount, width, height, mipLevels, sampleCount);
            }
            if (OverlayPtr == IntPtr.Zero)
            {
                throw new Exception(OculusOvr.GetError());
            }

            textures = new Texture[textureCount];
            for (var i = 0; i < textureCount; i++)
            {
                var ptr = OculusOvr.GetQuadLayerTextureDx(ovrSession, OverlayPtr, OculusOvrHmd.Dx11Texture2DGuid, i);
                if (ptr == IntPtr.Zero)
                {
                    throw new Exception(OculusOvr.GetError());
                }

                textures[i] = new Texture(device);
                unsafe
                {
                    textures[i].InitializeFromImpl(new ID3D11Texture2D((void**)ptr), false);
                }
            }
        }

        public override void Dispose()
        {
        }

        public override void UpdateSurface(CommandList commandList, Texture texture)
        {
            OculusOvr.SetQuadLayerParams(OverlayPtr, ref Position, ref Rotation, ref SurfaceSize, FollowHeadRotation);
            var index = OculusOvr.GetCurrentQuadLayerTargetIndex(ovrSession, OverlayPtr);
            commandList.Copy(texture, textures[index]);
        }
    }
}

#endif
