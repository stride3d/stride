// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Games;
using Xenko.Graphics;

namespace Xenko.VirtualReality
{
    public class VRDeviceSystem : GameSystemBase
    {
        private static bool physicalDeviceInUse;

        public VRDeviceSystem(IServiceRegistry registry) : base(registry)
        {
            EnabledChanged += OnEnabledChanged;

            DrawOrder = -100;
            UpdateOrder = -100;
        }

        public VRApi[] PreferredApis;

        public Dictionary<VRApi, float> PreferredScalings;

        public VRDevice Device { get; private set; }

        public bool RequireMirror;

        public int MirrorWidth;

        public int MirrorHeight;

        public bool PreviousUseCustomProjectionMatrix;

        public bool PreviousUseCustomViewMatrix;

        public Matrix PreviousCameraProjection;

        public float ResolutionScale;

        private void OnEnabledChanged(object sender, EventArgs eventArgs)
        {
            if (Enabled && Device == null)
            {
                if (PreferredApis == null)
                {
                    return;
                }

                if (physicalDeviceInUse)
                {
                    Device = null;
                    goto postswitch;
                }

                foreach (var hmdApi in PreferredApis)
                {
                    switch (hmdApi)
                    {
                        case VRApi.Dummy:
                        {
                            Device = new DummyDevice(Services);
                            break;
                        }
                        case VRApi.Oculus:
                        {
#if XENKO_GRAPHICS_API_DIRECT3D11
                            Device = new OculusOvrHmd();
                                
#endif
                            break;
                        }
                        case VRApi.OpenVR:
                        {
#if XENKO_GRAPHICS_API_DIRECT3D11
                            Device = new OpenVRHmd();
#endif
                            break;
                        }
                        case VRApi.WindowsMixedReality:
                        {
#if XENKO_GRAPHICS_API_DIRECT3D11 && XENKO_PLATFORM_UWP
                            if (Windows.Graphics.Holographic.HolographicSpace.IsAvailable && GraphicsDevice.Presenter is WindowsMixedRealityGraphicsPresenter)
                            {
                                Device = new WindowsMixedRealityHmd();
                            }
#endif
                            break;
                        }
                        //case VRApi.Fove:
                        //{
                        //#if XENKO_GRAPHICS_API_DIRECT3D11
                        //    Device = new FoveHmd();
                        //#endif
                        //break;
                        //}
                        //case VRApi.Google:
                        //{
                        //#if XENKO_PLATFORM_IOS || XENKO_PLATFORM_ANDROID
                        //    VRDevice = new GoogleVrHmd();
                        //#endif
                        //    break;
                        //}
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (Device != null)
                    {
                        Device.Game = Game;

                        if (Device != null && !Device.CanInitialize)
                        {
                            Device.Dispose();
                            Device = null;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

postswitch:

                var deviceManager = (GraphicsDeviceManager)Services.GetService<IGraphicsDeviceManager>();
                if (Device != null)
                {
                    Device.RenderFrameScaling = PreferredScalings[Device.VRApi];
                    Device.Enable(GraphicsDevice, deviceManager, RequireMirror, MirrorWidth, MirrorHeight);
                    physicalDeviceInUse = true;
                }
                else
                {
                    //fallback to dummy device
                    Device = new DummyDevice(Services)
                    {
                        Game = Game,
                        RenderFrameScaling = 1.0f,
                    };
                    Device.Enable(GraphicsDevice, deviceManager, RequireMirror, MirrorWidth, MirrorHeight);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            Device?.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Device?.Draw(gameTime);
        }

        protected override void Destroy()
        {
            if (Device != null)
            {
                if (!(Device is DummyDevice))
                {
                    physicalDeviceInUse = false;
                }

                Device.Dispose();
                Device = null;
            }
        }
    }
}
