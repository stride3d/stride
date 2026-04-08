#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.OpenXR;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Graphics;

#if STRIDE_GRAPHICS_API_DIRECT3D11
using Silk.NET.Direct3D11;
#endif

namespace Stride.VirtualReality
{
    public unsafe class OpenXRHmd : VRDevice
    {
        // Public static variable to add extensions to the initialization of the openXR session
        public static List<string> extensions = [];

        /// <summary>
        /// Creates a VR device using OpenXR.
        /// </summary>
        /// <param name="requestPassthrough">Whether or not the XR_FB_passthrough extension should be enabled (if available).</param>
        internal static OpenXRHmd? New(bool requestPassthrough)
        {
            // Create our API object for OpenXR.
            var xr = XR.GetApi();
            if (xr is null)
            {
                Logger.Debug("Failed to load OpenXR API");
                return null;
            }

            // Takes ownership on API object
            return new OpenXRHmd(xr, requestPassthrough);
        }

        // API Objects for accessing OpenXR
        public readonly XR Xr;

        // OpenXR handles
        public readonly Instance Instance;
        public readonly ulong SystemId;

        public Session globalSession;
        public Swapchain globalSwapchain;
        public Space globalPlaySpace;
        public FrameState globalFrameState;
        public ReferenceSpaceType play_space_type = ReferenceSpaceType.Local; //XR_REFERENCE_SPACE_TYPE_LOCAL;
#if STRIDE_GRAPHICS_API_DIRECT3D11
        public SwapchainImageD3D11KHR[]? images;
        public ID3D11RenderTargetView*[]? render_targets;
#endif
        public ActionSet globalActionSet;
        public InteractionProfileState handProfileState;
        private ulong leftHandPath;

        // Misc
        private static Logger Logger = GlobalLogger.GetLogger("OpenXRHmd");
        private bool runFramecycle = false;
        private bool sessionRunning = false;
        private SessionState state = SessionState.Unknown;

        // Passthrough
        private OpenXRExt_FB_Passthrough? passthroughExt;
        private bool passthroughRequested;

        private GraphicsDevice? baseDevice;

        private Size2 renderSize;

#if STRIDE_GRAPHICS_API_DIRECT3D11
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate Result pfnGetD3D11GraphicsRequirementsKHR(Instance instance, ulong sys_id, GraphicsRequirementsD3D11KHR* req);
#endif

        private bool begunFrame, swapImageCollected;
        private uint swapchainPointer;

        // array of view_count containers for submitting swapchains with rendered VR frames
        private CompositionLayerProjectionView[]? projection_views;
        private View[]? views;
        private readonly List<IntPtr> compositionLayers = [];

        public override Size2 ActualRenderFrameSize
        {
            get => renderSize;
            protected set => renderSize = value;
        }

        public override float RenderFrameScaling { get; set; } = 1f;

        public override DeviceState State => CanInitialize ? DeviceState.Valid : DeviceState.Invalid;

        private Vector3 headPos;
        public override Vector3 HeadPosition => headPos;

        private Quaternion headRot;
        public override Quaternion HeadRotation => headRot;

        public override Vector3 HeadLinearVelocity => default;

        public override Vector3 HeadAngularVelocity => default;

        private OpenXrTouchController? leftHand;
        public override TouchController? LeftHand => leftHand;

        private OpenXrTouchController? rightHand;
        public override TouchController? RightHand => rightHand;

        public override bool CanInitialize
        {
            get
            {
#if STRIDE_GRAPHICS_API_DIRECT3D11
                return SystemId != 0;
#else
                return false;
#endif
            }
        }

        public override bool CanMirrorTexture => false;

        public override Size2 OptimalRenderFrameSize => renderSize;

        // TODO (not implemented)
        private Texture? mirrorTexture;
        public override Texture? MirrorTexture
        {
            get => mirrorTexture;
            protected set => mirrorTexture = value;
        }

        public override TrackedItem[]? TrackedItems => null;

        private OpenXRHmd(XR xr, bool requestPassthrough)
        {
            Xr = xr;
            VRApi = VRApi.OpenXR;
            passthroughRequested = requestPassthrough;

            var requestedExtensions = new List<string>(extensions);
            if (requestPassthrough)
                requestedExtensions.Add(OpenXRUtils.XR_FB_PASSTHROUGH_EXTENSION_NAME);

            Instance = OpenXRUtils.CreateRuntime(xr, requestedExtensions, Logger);
            SystemId = Instance.Handle != 0 ? OpenXRUtils.GetSystem(xr, Instance, Logger) : default;
            SupportsPassthrough = requestPassthrough && Xr.IsInstanceExtensionPresent(null, OpenXRUtils.XR_FB_PASSTHROUGH_EXTENSION_NAME);
        }

        public override unsafe void Enable(GraphicsDevice device, GraphicsDeviceManager graphicsDeviceManager, bool requireMirror, int mirrorWidth, int mirrorHeight)
        {
            if (!CanInitialize)
                throw new InvalidOperationException();

            baseDevice = device;

            SystemProperties system_props = new SystemProperties()
            {
                Type = StructureType.SystemProperties,
                Next = null
            };

            // Changing the form_factor may require changing the view_type too.
            var view_type = ViewConfigurationType.PrimaryStereo;
            uint view_config_count = 0;

            Xr.EnumerateViewConfiguration(Instance, SystemId, 0, ref view_config_count, viewConfigurationTypes: null).CheckResult();

            var viewconfigs = new ViewConfigurationType[view_config_count];
            fixed (ViewConfigurationType* viewconfigspnt = &viewconfigs[0])
                Xr.EnumerateViewConfiguration(Instance, SystemId, view_config_count, ref view_config_count, viewconfigspnt).CheckResult();
            Logger.Debug("Available config types: ");
            var viewtype_found = false;
            for (int i = 0; i < view_config_count; i++)
            {
                Logger.Debug("    " + viewconfigs[i]);
                viewtype_found |= view_type == viewconfigs[i];
            }
            if (!viewtype_found)
            {
                throw new Exception("The current device doesn't support primary stereo");
            }

            uint view_count = 0;
            Xr.EnumerateViewConfigurationView(Instance, SystemId, view_type, 0, ref view_count, null).CheckResult();
            var viewconfig_views = new ViewConfigurationView[view_count];
            for (int i = 0; i < view_count; i++)
            {
                viewconfig_views[i].Type = StructureType.ViewConfigurationView;
                viewconfig_views[i].Next = null;
            }
            fixed (ViewConfigurationView* viewspnt = &viewconfig_views[0])
                Xr.EnumerateViewConfigurationView(Instance, SystemId, view_type, (uint)viewconfig_views.Length, ref view_count, viewspnt).CheckResult();

            // get size
            renderSize.Width = (int)Math.Round(viewconfig_views[0].RecommendedImageRectWidth * RenderFrameScaling) * 2; // 2 views in one frame
            renderSize.Height = (int)Math.Round(viewconfig_views[0].RecommendedImageRectHeight * RenderFrameScaling);

            SessionCreateInfo session_create_info;

#if STRIDE_GRAPHICS_API_DIRECT3D11
            Logger.Debug("Initializing DX11 graphics device: ");
            var dx11 = new GraphicsRequirementsD3D11KHR { Type = StructureType.GraphicsRequirementsD3D11Khr };

            var xrGetD3D11GraphicsRequirementsKHR = new PfnVoidFunction();
            Xr.GetInstanceProcAddr(Instance, "xrGetD3D11GraphicsRequirementsKHR", ref xrGetD3D11GraphicsRequirementsKHR).CheckResult();
            // this function pointer was loaded with xrGetInstanceProcAddr
            Delegate dx11_req = Marshal.GetDelegateForFunctionPointer((IntPtr)xrGetD3D11GraphicsRequirementsKHR.Handle, typeof(pfnGetD3D11GraphicsRequirementsKHR));
            dx11_req.DynamicInvoke(Instance, SystemId, new IntPtr(&dx11));
            Logger.Debug("Initializing dx11 graphics device");
            Logger.Debug(
                "DX11 device luid: " + dx11.AdapterLuid
                + " min feature level: " + dx11.MinFeatureLevel
            );

            var graphics_binding_dx11 = new GraphicsBindingD3D11KHR()
            {
                Type = StructureType.GraphicsBindingD3D11Khr,
                Device = baseDevice.NativeDevice,
                Next = null
            };
            session_create_info = new SessionCreateInfo()
            {
                Type = StructureType.SessionCreateInfo,
                Next = &graphics_binding_dx11,
                SystemId = SystemId
            };
#else
            throw new Exception("OpenXR is only compatible with DirectX11");
#endif

            Session session;
            Xr.CreateSession(Instance, &session_create_info, &session).CheckResult();
            globalSession = session;

            // --- Create swapchain for main VR rendering
            Swapchain swapchain = new Swapchain();
            SwapchainCreateInfo swapchain_create_info = new()
            {
                Type = StructureType.SwapchainCreateInfo,
                Next = null,
                UsageFlags = SwapchainUsageFlags.TransferDstBit |
                             SwapchainUsageFlags.SampledBit |
                             SwapchainUsageFlags.ColorAttachmentBit,
                CreateFlags = 0,
#if STRIDE_GRAPHICS_API_DIRECT3D11
                Format = (long)PixelFormat.R8G8B8A8_UNorm_SRgb,
#endif
                SampleCount = viewconfig_views[0].RecommendedSwapchainSampleCount,
                Width = (uint)renderSize.Width,
                Height = (uint)renderSize.Height,
                FaceCount = 1,
                ArraySize = 1,
                MipCount = 1
            };

            Xr.CreateSwapchain(session, &swapchain_create_info, &swapchain).CheckResult();
            globalSwapchain = swapchain;

            uint img_count = 0;
            Xr.EnumerateSwapchainImages(swapchain, 0, ref img_count, null).CheckResult();
#if STRIDE_GRAPHICS_API_DIRECT3D11
            images = new SwapchainImageD3D11KHR[img_count];
            for (int i = 0; i < img_count; i++)
            {
                images[i].Type = StructureType.SwapchainImageD3D11Khr;
                images[i].Next = null;
            }

            fixed (void* sibhp = &images[0])
            {
                Xr.EnumerateSwapchainImages(swapchain, img_count, ref img_count, (SwapchainImageBaseHeader*)sibhp).CheckResult();
            }

            render_targets = new ID3D11RenderTargetView*[img_count];
            for (var i = 0; i < img_count; ++i)
            {
                var texture = (ID3D11Texture2D*) images[i].Texture;

                Texture2DDesc color_desc;
                texture->GetDesc(&color_desc);

                Logger.Debug($"Color texture description: {color_desc.Width}x{color_desc.Height} format: {color_desc.Format}");

                var target_desc = new RenderTargetViewDesc
                {
                    ViewDimension = RtvDimension.Texture2D,
                    Format = Silk.NET.DXGI.Format.FormatR8G8B8A8UnormSrgb
                };

                ID3D11RenderTargetView* rtv;
                HResult result = baseDevice.NativeDevice.CreateRenderTargetView((ID3D11Resource*) texture, target_desc, &rtv);

                if (result.IsFailure)
                    result.Throw();

                render_targets[i] = rtv;
            }
#endif

            // Do not allocate these every frame to save some resources
            views = new View[view_count]; //(XrView*)malloc(sizeof(XrView) * view_count);
            for (int i = 0; i < view_count; i++)
            {
                views[i].Type = StructureType.View;
                views[i].Next = null;
            }

            projection_views = new CompositionLayerProjectionView[view_count]; //(XrCompositionLayerProjectionView*)malloc(sizeof(XrCompositionLayerProjectionView) * view_count);
            for (int i = 0; i < view_count; i++)
            {
                projection_views[i].Type = StructureType.CompositionLayerProjectionView; //XR_TYPE_COMPOSITION_LAYER_PROJECTION_VIEW;
                projection_views[i].Next = null;
                projection_views[i].SubImage.Swapchain = swapchain;
                projection_views[i].SubImage.ImageArrayIndex = 0;
                projection_views[i].SubImage.ImageRect.Offset.X = (renderSize.Width * i) / 2;
                projection_views[i].SubImage.ImageRect.Offset.Y = 0;
                projection_views[i].SubImage.ImageRect.Extent.Width = renderSize.Width / 2;
                projection_views[i].SubImage.ImageRect.Extent.Height = renderSize.Height;
                // projection_views[i].{pose, fov} have to be filled every frame in frame loop
            };

            ReferenceSpaceCreateInfo play_space_create_info = new ReferenceSpaceCreateInfo()
            {
                Type = StructureType.ReferenceSpaceCreateInfo,
                Next = null,
                ReferenceSpaceType = play_space_type,
                PoseInReferenceSpace = new Posef(new Quaternionf(0f, 0f, 0f, 1f), new Vector3f(0f, 0f, 0f))
            };
            var play_space = new Space();
            Xr.CreateReferenceSpace(session, &play_space_create_info, &play_space).CheckResult();
            globalPlaySpace = play_space;

            ActionSetCreateInfo gameplay_actionset_info = new ActionSetCreateInfo()
            {
                Type = StructureType.ActionSetCreateInfo,
                Next = null
            };

            Span<byte> asname = new Span<byte>(gameplay_actionset_info.ActionSetName, 16);
            Span<byte> lsname = new Span<byte>(gameplay_actionset_info.LocalizedActionSetName, 16);
            SilkMarshal.StringIntoSpan("actionset\0", asname);
            SilkMarshal.StringIntoSpan("ActionSet\0", lsname);

            ActionSet gameplay_actionset;
            Xr.CreateActionSet(Instance, &gameplay_actionset_info, &gameplay_actionset).CheckResult();
            globalActionSet = gameplay_actionset;

            var input = new OpenXRInput(this);

            leftHand = new OpenXrTouchController(this, input, TouchControllerHand.Left);
            rightHand = new OpenXrTouchController(this, input, TouchControllerHand.Right);

            SessionActionSetsAttachInfo actionset_attach_info = new SessionActionSetsAttachInfo()
            {
                Type = StructureType.SessionActionSetsAttachInfo,
                Next = null,
                CountActionSets = 1,
                ActionSets = &gameplay_actionset
            };

            Xr.AttachSessionActionSets(session, &actionset_attach_info).CheckResult();

            // figure out what interaction profile we are using, and determine if it has a touchpad/thumbstick or both
            handProfileState = new InteractionProfileState()
            {
                Type = StructureType.InteractionProfileState,
                Next = null
            };
            Xr.StringToPath(Instance, "/user/hand/left", ref leftHandPath);
        }

        public override IDisposable StartPassthrough()
        {
            if (!passthroughRequested)
                throw new InvalidOperationException("The passthrough mode needs to be enabled at device creation");

            if (!SupportsPassthrough)
                throw new NotSupportedException("The device does not support passthrough mode");

            if (passthroughExt is null)
                passthroughExt = new OpenXRExt_FB_Passthrough(Xr, globalSession, Instance);

            if (passthroughExt.Enabled)
                throw new InvalidOperationException("Passthrough already started");

            passthroughExt.Enabled = true;

            return new AnonymousDisposable(() => passthroughExt.Enabled = false);
        }

        private void EndNullFrame()
        {
            FrameEndInfo frame_end_info = new FrameEndInfo()
            {
                Type = StructureType.FrameEndInfo,
                DisplayTime = globalFrameState.PredictedDisplayTime,
                EnvironmentBlendMode = EnvironmentBlendMode.Opaque,
                LayerCount = 0,
                Layers = null,
                Next = null
            };
            Xr.EndFrame(globalSession, frame_end_info).CheckResult();
        }

        public override unsafe void Draw(GameTime gameTime)
        {
            if (!runFramecycle || !sessionRunning)
            {
                begunFrame = false;
                return;
            }

            // wait get poses (headPos etc.)
            // --- Wait for our turn to do head-pose dependent computation and render a frame
            FrameWaitInfo frame_wait_info = new FrameWaitInfo()
            {
                Type = StructureType.FrameWaitInfo
            };

            //Logger.Warning("WaitFrame");
            globalFrameState = new FrameState()
            {
                Type = StructureType.FrameState,
                Next = null
            };
            Xr.WaitFrame(globalSession, in frame_wait_info, ref globalFrameState).CheckResult();

            FrameBeginInfo frame_begin_info = new FrameBeginInfo()
            {
                Type = StructureType.FrameBeginInfo,
                Next = null
            };

            //Logger.Warning("BeginFrame");
            Xr.BeginFrame(globalSession, frame_begin_info).CheckResult();

            if ((Bool32)globalFrameState.ShouldRender)
            {
                swapchainPointer = GetSwapchainImage();
                UpdateViews();
                begunFrame = true;
            }
            else
            {
                EndNullFrame();
                begunFrame = false;
            }
        }

        public void UpdateViews()
        {
            if (views is null)
                return;

            // --- Create projection matrices and view matrices for each eye
            ViewLocateInfo view_locate_info = new ViewLocateInfo()
            {
                Type = StructureType.ViewLocateInfo,
                ViewConfigurationType = ViewConfigurationType.PrimaryStereo,
                DisplayTime = globalFrameState.PredictedDisplayTime,
                Space = globalPlaySpace,
                Next = null
            };

            ViewState view_state = new ViewState()
            {
                Type = StructureType.ViewState,
                Next = null
            };

            unsafe
            {
                uint view_count = 2;
                // Logger.Warning("LocateView");
                Xr.LocateView(globalSession, &view_locate_info, &view_state, 2, &view_count, views).CheckResult();
            }

            // get head rotation
            headRot.X = views[0].Pose.Orientation.X;
            headRot.Y = views[0].Pose.Orientation.Y;
            headRot.Z = views[0].Pose.Orientation.Z;
            headRot.W = views[0].Pose.Orientation.W;

            // since we got eye positions, our head is between our eyes
            headPos.X = views[0].Pose.Position.X;
            headPos.Y = views[0].Pose.Position.Y;
            headPos.Z = views[0].Pose.Position.Z;
        }

#if STRIDE_GRAPHICS_API_DIRECT3D11
        public unsafe uint GetSwapchainImage()
        {
            // Logger.Warning("AcquireSwapchainImage");
            // Get the swapchain image
            var swapchainIndex = 0u;
            var acquireInfo = new SwapchainImageAcquireInfo()
            {
                Type = StructureType.SwapchainImageAcquireInfo,
                Next = null
            };
            Xr.AcquireSwapchainImage(globalSwapchain, in acquireInfo, ref swapchainIndex).CheckResult();

            // Logger.Warning("WaitSwapchainImage");
            var waitInfo = new SwapchainImageWaitInfo(timeout: long.MaxValue)
            {
                Type = StructureType.SwapchainImageWaitInfo,
                Next = null
            };
            swapImageCollected = (Xr.WaitSwapchainImage(globalSwapchain, in waitInfo) == Result.Success);
            if(!swapImageCollected)
            {
                // Logger.Warning("WaitSwapchainImage failed");
                var releaseInfo = new SwapchainImageReleaseInfo()
                {
                    Type = StructureType.SwapchainImageReleaseInfo,
                    Next = null,
                };
                Xr.ReleaseSwapchainImage(globalSwapchain, in releaseInfo).CheckResult();
            }

            return swapchainIndex;
        }
#else
        public unsafe uint GetSwapchainImage()
        {
            throw new InvalidOperationException($"OpenXR error! Current implementation doesn't support directX 11");
        }
#endif

        public override void Commit(CommandList commandList, Texture renderFrame)
        {
            if (baseDevice is null || projection_views is null || views is null)
                return;

#if STRIDE_GRAPHICS_API_DIRECT3D11
            if (render_targets is null)
                return;
#endif

            // if we didn't wait a frame, don't commit
            if (begunFrame == false)
                return;

            begunFrame = false;

            if (swapImageCollected)
            {
#if STRIDE_GRAPHICS_API_DIRECT3D11
                Debug.Assert(commandList.NativeDeviceContext.EqualsComPtr(baseDevice.NativeDeviceContext));

                // Logger.Warning("Blit render target");
                ID3D11Resource* renderFrameResource;
                renderFrame.NativeRenderTargetView.GetResource(&renderFrameResource);

                ID3D11Resource* swapChainRenderTargetResource;
                render_targets[swapchainPointer]->GetResource(&swapChainRenderTargetResource);

                baseDevice.NativeDeviceContext.CopyResource(renderFrameResource, swapChainRenderTargetResource);
#endif

                // Release the swapchain image
                // Logger.Warning("ReleaseSwapchainImage");
                var releaseInfo = new SwapchainImageReleaseInfo()
                {
                    Type = StructureType.SwapchainImageReleaseInfo,
                    Next = null,
                };
                Xr.ReleaseSwapchainImage(globalSwapchain, in releaseInfo).CheckResult();

                for (var eye = 0; eye < 2; eye++)
                {
                    projection_views[eye].Fov = views[eye].Fov;
                    projection_views[eye].Pose = views[eye].Pose;
                }

                unsafe
                {
                    // Add composition layers from extensions
                    compositionLayers.Clear();
                    if (passthroughExt != null && passthroughExt.Enabled)
                    {
                        var layer = passthroughExt.GetCompositionLayer();
                        compositionLayers.Add(new IntPtr(layer));
                    }

                    fixed (CompositionLayerProjectionView* projection_views_ptr = &projection_views[0])
                    {
                        var projectionLayer = new CompositionLayerProjection
                        (
                            viewCount: (uint)projection_views.Length,
                            views: projection_views_ptr,
                            space: globalPlaySpace,
                            layerFlags: compositionLayers.Count > 0 ? CompositionLayerFlags.BlendTextureSourceAlphaBit : 0
                        );

                        compositionLayers.Add(new IntPtr(&projectionLayer));
                        fixed (nint* layersPtr = CollectionsMarshal.AsSpan(compositionLayers))
                        {
                            var frameEndInfo = new FrameEndInfo()
                            {
                                Type = StructureType.FrameEndInfo,
                                DisplayTime = globalFrameState.PredictedDisplayTime,
                                EnvironmentBlendMode = EnvironmentBlendMode.Opaque,
                                LayerCount = (uint)compositionLayers.Count,
                                Layers = (CompositionLayerBaseHeader**)layersPtr,
                                Next = null,
                            };

                            //Logger.Warning("EndFrame");
                            Xr.EndFrame(globalSession, in frameEndInfo).CheckResult();
                        }
                    }
                }
            }
            else
            {
                EndNullFrame();
            }
        }


        public override void SetTrackingSpace(TrackingSpace space)
        {
            Logger.Debug("Changing tracking space to: " + space);
            switch (space)
            {
                case TrackingSpace.Seated:
                    play_space_type = ReferenceSpaceType.Local;
                    break;
                case TrackingSpace.Standing:
                    play_space_type = ReferenceSpaceType.Stage;
                    break;
                case TrackingSpace.RawAndUncalibrated:
                    play_space_type = ReferenceSpaceType.View;
                    break;
            }

            if (!globalPlaySpace.Equals(null))
            {
                Xr.DestroySpace(globalPlaySpace).CheckResult();
            }

            var play_space = new Space();
            ReferenceSpaceCreateInfo play_space_create_info = new ReferenceSpaceCreateInfo()
            {
                Type = StructureType.ReferenceSpaceCreateInfo,
                Next = null,
                ReferenceSpaceType = play_space_type,
                PoseInReferenceSpace = new Posef(new Quaternionf(0f, 0f, 0f, 1f), new Vector3f(0f, 0f, 0f))
            };
            unsafe
            {
                Xr.CreateReferenceSpace(globalSession, &play_space_create_info, &play_space).CheckResult();
            }
            globalPlaySpace = play_space;
        }

        public override void Recenter()
        {
            // TODO: OpenXR doens´t have a renceter api. Recenter in this case needs to be done from the
            // engine by moving the world or adding an offset?
            //
            // The VR api could have a new property CanRencenter or DoesRecenter that returns true or false
            // if the specific API can do a renceter or the engine needs to take care of that
        }

        private static Matrix CreateViewMatrix(Vector3 translation, Quaternion rotation)
        {
            Matrix rotationMatrix = Matrix.RotationQuaternion(rotation);
            Matrix translationMatrix = Matrix.Translation(translation);
            Matrix viewMatrix = translationMatrix * rotationMatrix;
            viewMatrix.Invert();
            return viewMatrix;
        }

        private Matrix CreateProjectionFov(Fovf fov, float nearZ, float farZ)
        {
            Matrix result = Matrix.Identity;

            float tanAngleLeft = (float)Math.Tan(fov.AngleLeft);
            float tanAngleRight = (float)Math.Tan(fov.AngleRight);

            float tanAngleDown = (float)Math.Tan(fov.AngleDown);
            float tanAngleUp = (float)Math.Tan(fov.AngleUp);

            float tanAngleWidth = tanAngleRight - tanAngleLeft;
            float tanAngleHeight = (tanAngleUp - tanAngleDown);

            float offsetZ = 0;

            if (farZ <= nearZ)
            {
                // place the far plane at infinity
                result[0] = 2 / tanAngleWidth;
                result[4] = 0;
                result[8] = (tanAngleRight + tanAngleLeft) / tanAngleWidth;
                result[12] = 0;

                result[1] = 0;
                result[5] = 2 / tanAngleHeight;
                result[9] = (tanAngleUp + tanAngleDown) / tanAngleHeight;
                result[13] = 0;

                result[2] = 0;
                result[6] = 0;
                result[10] = -1;
                result[14] = -(nearZ + offsetZ);

                result[3] = 0;
                result[7] = 0;
                result[11] = -1;
                result[15] = 0;
            }
            else
            {
                // normal projection
                result[0] = 2 / tanAngleWidth;
                result[4] = 0;
                result[8] = (tanAngleRight + tanAngleLeft) / tanAngleWidth;
                result[12] = 0;

                result[1] = 0;
                result[5] = 2 / tanAngleHeight;
                result[9] = (tanAngleUp + tanAngleDown) / tanAngleHeight;
                result[13] = 0;

                result[2] = 0;
                result[6] = 0;
                result[10] = -(farZ + offsetZ) / (farZ - nearZ);
                result[14] = -(farZ * (nearZ + offsetZ)) / (farZ - nearZ);

                result[3] = 0;
                result[7] = 0;
                result[11] = -1;
                result[15] = 0;
            }

            return result;
        }

        private static Quaternion ConvertToFocus(in Quaternionf quat)
        {
            return new Quaternion(-quat.X, -quat.Y, -quat.Z, quat.W);
        }

        public override void ReadEyeParameters(Eyes eye, float near, float far, ref Vector3 cameraPosition, ref Matrix cameraRotation, bool ignoreHeadRotation, bool ignoreHeadPosition, out Matrix view, out Matrix projection)
        {
            if (views is null)
                throw new InvalidOperationException();

            Matrix eyeMat, rot;
            Vector3 pos, scale;

            View eyeview = views[(int)eye];

            projection = CreateProjectionFov(eyeview.Fov, near, far);
            var adjustedHeadMatrix = CreateViewMatrix(new Vector3(-eyeview.Pose.Position.X, -eyeview.Pose.Position.Y, -eyeview.Pose.Position.Z),
                                                      ConvertToFocus(in eyeview.Pose.Orientation));
            if (ignoreHeadPosition)
            {
                adjustedHeadMatrix.TranslationVector = Vector3.Zero;
            }
            if (ignoreHeadRotation)
            {
                // keep the scale just in case
                adjustedHeadMatrix.Row1 = new Vector4(adjustedHeadMatrix.Row1.Length(), 0, 0, 0);
                adjustedHeadMatrix.Row2 = new Vector4(0, adjustedHeadMatrix.Row2.Length(), 0, 0);
                adjustedHeadMatrix.Row3 = new Vector4(0, 0, adjustedHeadMatrix.Row3.Length(), 0);
            }

            eyeMat = adjustedHeadMatrix * /*Matrix.Scaling(BodyScaling) */ cameraRotation * Matrix.Translation(cameraPosition);
            eyeMat.Decompose(out scale, out rot, out pos);
            var finalUp = Vector3.TransformCoordinate(new Vector3(0, 1, 0), rot);
            var finalForward = Vector3.TransformCoordinate(new Vector3(0, 0, -1), rot);
            view = Matrix.LookAtRH(pos, pos + finalForward, finalUp);
        }

        public override unsafe void Update(GameTime gameTime)
        {
            var runtime_event = new EventDataBuffer()
            {
                Type = StructureType.EventDataBuffer,
                Next = null
            };
            while (Xr.PollEvent(Instance, &runtime_event) == Result.Success)
            {
                switch (runtime_event.Type)
                {
                    case StructureType.EventDataInstanceLossPending:
                        {
                            var loss_event = Unsafe.As<EventDataBuffer, EventDataInstanceLossPending>(ref runtime_event);
                            Logger.Warning("EVENT: instance loss pending at " + loss_event.LossTime + ". Destroying instance.");
                            break;
                        }
                    case StructureType.EventDataSessionStateChanged:
                        {
                            var session_event = Unsafe.As<EventDataBuffer, EventDataSessionStateChanged>(ref runtime_event);
                            Logger.Debug("EVENT: session state changed " + state + " -> " + session_event.State);
                            state = session_event.State;
                            switch (session_event.State)
                            {
                                // skip render loop, keep polling
                                case SessionState.Idle:
                                case SessionState.Unknown:
                                    {
                                        runFramecycle = false;
                                        break; // state handling switch
                                    }
                                case SessionState.Ready:
                                    {
                                        if (!sessionRunning)
                                        {
                                            SessionBeginInfo session_begin_info = new SessionBeginInfo()
                                            {
                                                Type = StructureType.SessionBeginInfo,
                                                Next = null,
                                                PrimaryViewConfigurationType = ViewConfigurationType.PrimaryStereo,
                                            };
                                            Xr.BeginSession(globalSession, &session_begin_info).CheckResult();
                                            sessionRunning = true;
                                        }
                                        runFramecycle = true;
                                        break;
                                    }
                                case SessionState.Stopping:
                                    {
                                        if(sessionRunning)
                                        {
                                            Xr.EndSession(globalSession).CheckResult();
                                            sessionRunning = false;
                                        }
                                        runFramecycle = false;
                                        break;
                                    }
                                case SessionState.LossPending:
                                case SessionState.Exiting:
                                    {
                                        Xr.DestroySession(globalSession).CheckResult();
                                        runFramecycle = false;
                                        break;
                                    }
                            }
                            break;
                        }
                    case StructureType.EventDataInteractionProfileChanged:
                        {
                            Logger.Debug("EVENT: interaction profile changed");
                            var profile_changed_event = Unsafe.As<EventDataBuffer, EventDataInteractionProfileChanged>(ref runtime_event);
                            Xr.GetCurrentInteractionProfile(profile_changed_event.Session, leftHandPath, ref handProfileState).CheckResult();
                            Logger.Debug(
                                "Profile changed to" + handProfileState.InteractionProfile.ToString()
                            );
                            break;
                        }
                    default:
                        {
                            Logger.Debug("EVENT: other type: " + runtime_event.Type);
                            break;
                        }
                }
                runtime_event.Type = StructureType.EventDataBuffer;
            }


            if (state != SessionState.Focused)
            {
                return;
            }
            ActiveActionSet active_actionsets = new ActiveActionSet()
            {
                ActionSet = globalActionSet
            };

            ActionsSyncInfo actions_sync_info = new ActionsSyncInfo()
            {
                Type = StructureType.ActionsSyncInfo,
                Next = null,
                CountActiveActionSets = 1,
                ActiveActionSets = &active_actionsets
            };

            Xr.SyncAction(globalSession, &actions_sync_info);

            leftHand?.Update(gameTime);
            rightHand?.Update(gameTime);
        }

        public override void Dispose()
        {
#if STRIDE_GRAPHICS_API_DIRECT3D11
            if (render_targets != null)
            {
                foreach (var render_target in render_targets)
                {
                    render_target->Release();
                }
            }
#endif

            if (globalPlaySpace.Handle != 0)
                Xr.DestroySpace(globalPlaySpace).CheckResult();

            if (globalActionSet.Handle != 0)
                Xr.DestroyActionSet(globalActionSet).CheckResult();

            if (globalSwapchain.Handle != 0)
                Xr.DestroySwapchain(globalSwapchain).CheckResult();

            passthroughExt?.Destroy();

            if (globalSession.Handle != 0)
                Xr.DestroySession(globalSession).CheckResult();

            if (Instance.Handle != 0)
                Xr.DestroyInstance(Instance).CheckResult();

            Xr.Dispose();
        }
    }
}
