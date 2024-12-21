// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace Stride.VirtualReality
{
    public abstract class VRDevice : IDisposable
    {
        public GameBase Game { get; internal set; }

        protected VRDevice()
        {
            ViewScaling = 1.0f;
        }

        public abstract Size2 OptimalRenderFrameSize { get; }

        public abstract Size2 ActualRenderFrameSize { get; protected set; }

        public abstract Texture MirrorTexture { get; protected set; }

        public abstract float RenderFrameScaling { get; set; }

        public abstract DeviceState State { get; }

        public abstract Vector3 HeadPosition { get; }

        public abstract Quaternion HeadRotation { get; }

        public abstract Vector3 HeadLinearVelocity { get; }

        public abstract Vector3 HeadAngularVelocity { get; }

        public abstract TouchController LeftHand { get; }

        public abstract TouchController RightHand { get; }

        public abstract TrackedItem[] TrackedItems { get; }

        public VRApi VRApi { get; protected set; }

        /// <summary>
        /// Allows you to scale the view, effectively it will change the size of the player in respect to the world, turning it into a giant or a tiny ant.
        /// </summary>
        /// <remarks>This will reduce the near clip plane of the cameras, it might induce depth issues.</remarks>
        public float ViewScaling { get; set; }

        public abstract bool CanInitialize { get; }

        public virtual bool CanMirrorTexture { get; } = true;

        public bool SupportsOverlays { get; protected set; } = false;

        /// <summary>
        /// Whether or not passthrough is supported by the device. 
        /// It needs to be requested on device creation by enabling <see cref="VRDeviceSystem.RequestPassthrough"/>.
        /// If supported, passthrough can be started (and stopped) with the <see cref="StartPassthrough"/> method.
        /// </summary>
        public bool SupportsPassthrough { get; protected set; } = false;

        public virtual VROverlay CreateOverlay(int width, int height, int mipLevels, int sampleCount)
        {
            return null;
        }

        public virtual void ReleaseOverlay(VROverlay overlay)
        {         
        }

        /// <summary>
        /// Starts a passthrough. When enabled the scene is rendered on top of the camera image of the device.
        /// </summary>
        /// <returns>A disposable which will stop the passthrough on dispose.</returns>
        /// <exception cref="NotSupportedException">Thrown if the passthrough mode is not supported by the device.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the passthrough mode is already enabled.</exception>
        public virtual IDisposable StartPassthrough()
        {
            throw new NotSupportedException();
        }

        public abstract void Enable(GraphicsDevice device, GraphicsDeviceManager graphicsDeviceManager, bool requireMirror, int mirrorWidth, int mirrorHeight);

        public virtual void Recenter()
        {
        }

        public virtual void SetTrackingSpace(TrackingSpace space)
        {
        }

        public abstract void ReadEyeParameters(Eyes eye, float near, float far, ref Vector3 cameraPosition, ref Matrix cameraRotation, bool ignoreHeadRotation, bool ignoreHeadPosition, out Matrix view, out Matrix projection);

        public abstract void Commit(CommandList commandList, Texture renderFrame);

        public virtual void Dispose()
        {           
        }

        public abstract void Update(GameTime gameTime);

        public abstract void Draw(GameTime gameTime);
    }
}
