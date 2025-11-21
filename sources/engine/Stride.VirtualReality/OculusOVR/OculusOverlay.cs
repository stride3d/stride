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

        public unsafe OculusOverlay(IntPtr ovrSession, GraphicsDevice device, int width, int height, int mipLevels, int sampleCount)
        {
            this.ovrSession = ovrSession;

            OverlayPtr = OculusOvr.CreateQuadLayerTexturesDx(ovrSession, (nint) device.NativeDevice.Handle, out var textureCount, width, height, mipLevels, sampleCount);
            if (OverlayPtr == IntPtr.Zero)
            {
                throw new Exception(OculusOvr.GetError());
            }

            textures = new Texture[textureCount];
            for (var i = 0; i < textureCount; i++)
            {
                var dxTexture2D = (ID3D11Texture2D*) OculusOvr.GetQuadLayerTextureDx(ovrSession, OverlayPtr, OculusOvrHmd.Dx11Texture2DGuid, i);
                if (dxTexture2D is null)
                {
                    throw new Exception(OculusOvr.GetError());
                }

                textures[i] = new Texture(device).InitializeFromImpl(dxTexture2D, treatAsSrgb: false);

                // We don't need to take ownership of the COM pointer.
                //   We are already AddRef()ing in Texture.InitializeFromImpl when storing the COM pointer;
                //   compensate with Release() to return the reference count to its previous value
                dxTexture2D->Release();
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
