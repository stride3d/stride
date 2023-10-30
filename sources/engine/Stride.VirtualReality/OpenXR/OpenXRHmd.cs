using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Silk.NET.OpenXR;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using System.Diagnostics;
using Silk.NET.Core.Native;
using System.Runtime.CompilerServices;

namespace Stride.VirtualReality
{
    public class OpenXRHmd : VRDevice
    {
        // API Objects for accessing OpenXR
        public XR Xr;
        public Session globalSession;
        public Swapchain globalSwapchain;
        public Space globalPlaySpace;
        public FrameState globalFrameState;
        public ReferenceSpaceType play_space_type = ReferenceSpaceType.Local; //XR_REFERENCE_SPACE_TYPE_LOCAL;
#if STRIDE_GRAPHICS_API_DIRECT3D11
        public SwapchainImageD3D11KHR[] images;
        public SharpDX.Direct3D11.RenderTargetView[] render_targets;
#endif
        // Public static variable to add extensions to the initialization of the openXR session
        public static List<string> extensions = new List<string>();
        public ActionSet globalActionSet;
        public InteractionProfileState handProfileState;
        internal ulong leftHandPath;

        // OpenXR handles
        public Instance Instance;
        public ulong system_id = 0;

        // Misc
        private bool _unmanagedResourcesFreed;
        private static Stride.Core.Diagnostics.Logger Logger = Stride.Core.Diagnostics.GlobalLogger.GetLogger("OpenXRHmd");
        private bool runFramecycle = false;
        private bool sessionRunning = false;
        private SessionState state = SessionState.Unknown;

        private GraphicsDevice baseDevice;

        private Size2 renderSize;

#if STRIDE_GRAPHICS_API_DIRECT3D11
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate Result pfnGetD3D11GraphicsRequirementsKHR(Instance instance, ulong sys_id, GraphicsRequirementsD3D11KHR* req);
#endif

        private unsafe delegate Result pfnCreateDebugUtilsMessengerEXT(Instance instance, DebugUtilsMessengerCreateInfoEXT* createInfo, DebugUtilsMessengerEXT* messenger);
        private unsafe delegate Result pfnDestroyDebugUtilsMessengerEXT(DebugUtilsMessengerEXT messenger);


        internal bool begunFrame, swapImageCollected;
        internal uint swapchainPointer;

        // array of view_count containers for submitting swapchains with rendered VR frames
        CompositionLayerProjectionView[] projection_views;
        View[] views;

        public override Size2 ActualRenderFrameSize
        {
            get => renderSize;
            protected set
            {
                renderSize = value;
            }
        }

        public override float RenderFrameScaling { get; set; } = 1f;

        public override DeviceState State
        {
            get
            {
                if (Xr == null) return DeviceState.Invalid;
                return DeviceState.Valid;
            }
        }

        private Vector3 headPos;
        public override Vector3 HeadPosition => headPos;

        private Quaternion headRot;
        public override Quaternion HeadRotation => headRot;

        private Vector3 headLinVel;
        public override Vector3 HeadLinearVelocity => headLinVel;

        private Vector3 headAngVel;
        public override Vector3 HeadAngularVelocity => headAngVel;

        private OpenXrTouchController leftHand;
        public override TouchController LeftHand => leftHand;

        private OpenXrTouchController rightHand;
        public override TouchController RightHand => rightHand;

        public override bool CanInitialize => true;

        public override bool CanMirrorTexture => false;

        public override Size2 OptimalRenderFrameSize => renderSize;

        // TODO (not implemented)
        private Texture mirrorTexture;
        public override Texture MirrorTexture { get => mirrorTexture; protected set => mirrorTexture = value; }

        public override TrackedItem[] TrackedItems => null;

        public OpenXRHmd(GraphicsDevice gd)
        {
            //baseDevice = gd;
            VRApi = VRApi.OpenXR;
        }

        /// <summary>
        /// A simple function which throws an exception if the given OpenXR result indicates an error has been raised.
        /// </summary>
        /// <param name="result">The OpenXR result in question.</param>
        /// <returns>
        /// The same result passed in, just in case it's meaningful and we just want to use this to filter out errors.
        /// </returns>
        /// <exception cref="Exception">An exception for the given result if it indicates an error.</exception>
        [DebuggerHidden]
        [DebuggerStepThrough]
        protected internal static Result CheckResult(Result result, string forFunction)
        {
            if ((int)result < 0)
                throw new InvalidOperationException($"OpenXR error! Make sure a OpenXR runtime is set & running (like SteamVR)\n\nCode: {result} ({result:X}) in " + forFunction + "\n\nStack Trace: " + (new StackTrace()).ToString());

            return result;
        }



        public override unsafe void Enable(GraphicsDevice device, GraphicsDeviceManager graphicsDeviceManager, bool requireMirror, int mirrorWidth, int mirrorHeight)
        {
            // Changing the form_factor may require changing the view_type too.
            ViewConfigurationType view_type = ViewConfigurationType.PrimaryStereo;

            baseDevice = device;
            // Create our API object for OpenXR.
            Xr = XR.GetApi();

            PrintApiLayers();

            Logger.Debug("Installing extensions");

            var openXrExtensions = new List<String>();
#if STRIDE_GRAPHICS_API_DIRECT3D11
            openXrExtensions.Add("XR_KHR_D3D11_enable");
#endif
#if DEBUG_OPENXR
            openXrExtensions.Add("XR_EXT_debug_utils");
#endif
            openXrExtensions.AddRange(extensions);

            uint propCount = 0;
            Xr.EnumerateInstanceExtensionProperties((byte*)null, 0, &propCount, null);

            ExtensionProperties[] props = new ExtensionProperties[propCount];
            for (int i = 0; i < props.Length; i++)
            {
                props[i].Type = StructureType.TypeExtensionProperties;
                props[i].Next = null;
            }
            Xr.EnumerateInstanceExtensionProperties((byte*)null, propCount, &propCount, props);

            Logger.Debug("Supported extensions (" + propCount + "):");
            List<string> AvailableExtensions = new List<string>();
            for (int i = 0; i < props.Length; i++)
            {
                fixed (void* nptr = props[i].ExtensionName)
                {
                    var extension_name = Marshal.PtrToStringAnsi(new System.IntPtr(nptr));
                    Logger.Debug(extension_name);
                    AvailableExtensions.Add(extension_name);
                }
            }

            for (int i = 0; i < openXrExtensions.Count; i++)
            {
                if (!AvailableExtensions.Contains(openXrExtensions[i]))
                {
                    openXrExtensions.RemoveAt(i);
                    i--;
                }
            }

            Logger.Debug("Available extensions of those enabled");
            for (int i = 0; i < openXrExtensions.Count; i++)
            {
                Logger.Debug(openXrExtensions[i]);
            }

#if STRIDE_GRAPHICS_API_DIRECT3D11
            if (!AvailableExtensions.Contains("XR_KHR_D3D11_enable"))
            {
                throw new InvalidOperationException($"OpenXR error! Current implementation doesn't support Direct3D 11");
            }
#endif

            var appInfo = new ApplicationInfo()
            {
                ApiVersion = new Version64(1, 0, 10)
            };

            // We've got to marshal our strings and put them into global, immovable memory. To do that, we use
            // SilkMarshal.
            Span<byte> appName = new Span<byte>(appInfo.ApplicationName, 128);
            Span<byte> engName = new Span<byte>(appInfo.EngineName, 128);
            SilkMarshal.StringIntoSpan(System.AppDomain.CurrentDomain.FriendlyName, appName);
            SilkMarshal.StringIntoSpan("Stride", engName);

            var requestedExtensions = SilkMarshal.StringArrayToPtr(openXrExtensions);
            InstanceCreateInfo instanceCreateInfo = new InstanceCreateInfo
            (
                applicationInfo: appInfo,
                enabledExtensionCount: (uint)openXrExtensions.Count,
                enabledExtensionNames: (byte**)requestedExtensions,
                createFlags: 0,
                enabledApiLayerCount: 0,
                enabledApiLayerNames: null
            );

            // Now we're ready to make our instance!
            CheckResult(Xr.CreateInstance(in instanceCreateInfo, ref Instance), "CreateInstance");

#if DEBUG_OPENXR
            Silk.NET.Core.PfnVoidFunction xrCreateDebugUtilsMessengerEXT = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(Xr.GetInstanceProcAddr(Instance, "xrCreateDebugUtilsMessengerEXT", ref xrCreateDebugUtilsMessengerEXT), "GetInstanceProcAddr::xrCreateDebugUtilsMessengerEXT");
            Delegate create_debug_utils_messenger = Marshal.GetDelegateForFunctionPointer((IntPtr)xrCreateDebugUtilsMessengerEXT.Handle, typeof(pfnCreateDebugUtilsMessengerEXT));

            // https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#debug-message-categorization
            DebugUtilsMessengerCreateInfoEXT debug_info = new DebugUtilsMessengerCreateInfoEXT()
            {
                Type = StructureType.TypeDebugUtilsMessengerCreateInfoExt,
                MessageTypes = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt
                    | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt
                    | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt
                    | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeConformanceBitExt,
                MessageSeverities = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt
                    | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt
                    | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt
                    | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt,
                UserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback,
            };

            DebugUtilsMessengerEXT xr_debug;
            var result = create_debug_utils_messenger.DynamicInvoke(Instance, new System.IntPtr(&debug_info), new System.IntPtr(&xr_debug));
#endif

            // This crashes on oculus
            // For our benefit, let's log some information about the instance we've just created.
            InstanceProperties properties = new InstanceProperties()
            {
                Type = StructureType.TypeInstanceProperties,
                Next = null,
            };
            CheckResult(Xr.GetInstanceProperties(Instance, ref properties), "GetInstanceProperties");

            var runtimeName = Marshal.PtrToStringAnsi(new System.IntPtr(properties.RuntimeName));
            var runtimeVersion = ((Version)(Version64)properties.RuntimeVersion).ToString(3);

            Logger.Info($"Using OpenXR Runtime \"{runtimeName}\" v{runtimeVersion}");

            // We're creating a head-mounted-display (HMD, i.e. a VR headset) example, so we ask for a runtime which
            // supports that form factor. The response we get is a ulong that is the System ID.
            var getInfo = new SystemGetInfo(formFactor: FormFactor.HeadMountedDisplay) { Type = StructureType.TypeSystemGetInfo };
            CheckResult(Xr.GetSystem(Instance, in getInfo, ref system_id), "GetSystem");
            Logger.Debug("Successfully got XrSystem with id " + system_id + " for HMD form factor");

            SystemProperties system_props = new SystemProperties()
            {
                Type = StructureType.TypeSystemProperties,
                Next = null,
            };

            // CheckResult(Xr.GetSystemProperties(Instance, system_id, &system_props), "GetSystemProperties");

            uint view_config_count = 0;
            CheckResult(Xr.EnumerateViewConfiguration(Instance, system_id, 0, ref view_config_count, null), "EnumerateViewConfiguration");
            var viewconfigs = new ViewConfigurationType[view_config_count];
            fixed (ViewConfigurationType* viewconfigspnt = &viewconfigs[0])
                CheckResult(Xr.EnumerateViewConfiguration(Instance, system_id, view_config_count, ref view_config_count, viewconfigspnt), "EnumerateViewConfiguration");
            Logger.Debug("Available config types: ");
            var viewtype_found = false;
            for (int i = 0; i < view_config_count; i++)
            {
                Logger.Debug("    " + viewconfigs[i]);
                viewtype_found |= view_type == viewconfigs[i];
            }
            if (!viewtype_found)
            {
                throw new Exception("The current device doesn´t support primary stereo");
            }

            uint view_count = 0;
            CheckResult(Xr.EnumerateViewConfigurationView(Instance, system_id, view_type, 0, ref view_count, null), "EnumerateViewConfigurationView");
            var viewconfig_views = new ViewConfigurationView[view_count];
            for (int i = 0; i < view_count; i++)
            {
                viewconfig_views[i].Type = StructureType.TypeViewConfigurationView;
                viewconfig_views[i].Next = null;
            }
            fixed (ViewConfigurationView* viewspnt = &viewconfig_views[0])
                CheckResult(Xr.EnumerateViewConfigurationView(Instance, system_id, view_type, (uint)viewconfig_views.Length, ref view_count, viewspnt), "EnumerateViewConfigurationView");

            // get size
            renderSize.Width = (int)Math.Round(viewconfig_views[0].RecommendedImageRectWidth * RenderFrameScaling) * 2; // 2 views in one frame
            renderSize.Height = (int)Math.Round(viewconfig_views[0].RecommendedImageRectHeight * RenderFrameScaling);

            SessionCreateInfo session_create_info;
#if STRIDE_GRAPHICS_API_DIRECT3D11
            Logger.Debug(
                "Initializing DX11 graphics device: "
            );
            GraphicsRequirementsD3D11KHR dx11 = new GraphicsRequirementsD3D11KHR()
            {
                Type = StructureType.TypeGraphicsRequirementsD3D11Khr
            };

            Silk.NET.Core.PfnVoidFunction xrGetD3D11GraphicsRequirementsKHR = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(Xr.GetInstanceProcAddr(Instance, "xrGetD3D11GraphicsRequirementsKHR", ref xrGetD3D11GraphicsRequirementsKHR), "GetInstanceProcAddr::xrGetD3D11GraphicsRequirementsKHR");
            // this function pointer was loaded with xrGetInstanceProcAddr
            Delegate dx11_req = Marshal.GetDelegateForFunctionPointer((IntPtr)xrGetD3D11GraphicsRequirementsKHR.Handle, typeof(pfnGetD3D11GraphicsRequirementsKHR));
            dx11_req.DynamicInvoke(Instance, system_id, new System.IntPtr(&dx11));
            Logger.Debug("Initializing dx11 graphics device");
            Logger.Debug(
                "DX11 device luid: " + dx11.AdapterLuid
                + " min feature level: " + dx11.MinFeatureLevel
            );

            var graphics_binding_dx11 = new GraphicsBindingD3D11KHR()
            {
                Type = StructureType.TypeGraphicsBindingD3D11Khr,
                Device = baseDevice.NativeDevice.NativePointer.ToPointer(),
                Next = null,
            };
            session_create_info = new SessionCreateInfo()
            {
                Type = StructureType.TypeSessionCreateInfo,
                Next = &graphics_binding_dx11,
                SystemId = system_id
            };
#else
            throw new Exception("OpenXR is only compatible with DirectX11");
#endif

            Session session;
            CheckResult(Xr.CreateSession(Instance, &session_create_info, &session), "CreateSession");
            globalSession = session;

            // --- Create swapchain for main VR rendering
            Swapchain swapchain = new Swapchain();
            SwapchainCreateInfo swapchain_create_info = new SwapchainCreateInfo()
            {
                Type = StructureType.TypeSwapchainCreateInfo,
                Next = null,
                UsageFlags = SwapchainUsageFlags.SwapchainUsageTransferDstBit |
                                SwapchainUsageFlags.SwapchainUsageSampledBit |
                                SwapchainUsageFlags.SwapchainUsageColorAttachmentBit,
                CreateFlags = 0,
#if STRIDE_GRAPHICS_API_DIRECT3D11
                Format = (long)PixelFormat.R8G8B8A8_UNorm_SRgb,
#endif
                SampleCount = viewconfig_views[0].RecommendedSwapchainSampleCount,
                Width = (uint)renderSize.Width,
                Height = (uint)renderSize.Height,
                FaceCount = 1,
                ArraySize = 1,
                MipCount = 1,
            };

            CheckResult(Xr.CreateSwapchain(session, &swapchain_create_info, &swapchain), "CreateSwapchain");
            globalSwapchain = swapchain;

            uint img_count = 0;
            CheckResult(Xr.EnumerateSwapchainImages(swapchain, 0, ref img_count, null), "EnumerateSwapchainImages");
#if STRIDE_GRAPHICS_API_DIRECT3D11
            images = new SwapchainImageD3D11KHR[img_count];
            for (int i = 0; i < img_count; i++)
            {
                images[i].Type = StructureType.TypeSwapchainImageD3D11Khr;
                images[i].Next = null;
            }

            fixed (void* sibhp = &images[0])
            {
                CheckResult(Xr.EnumerateSwapchainImages(swapchain, img_count, ref img_count, (SwapchainImageBaseHeader*)sibhp), "EnumerateSwapchainImages");
            }

            render_targets = new SharpDX.Direct3D11.RenderTargetView[img_count];
            for (var i = 0; i < img_count; ++i)
            {
                var texture = new SharpDX.Direct3D11.Texture2D((IntPtr)images[i].Texture);
                var color_desc = texture.Description;
                Logger.Debug("Color texture description: " + color_desc.Width.ToString() + "x" + color_desc.Height.ToString() + " format: " + color_desc.Format.ToString());

                var target_desc = new SharpDX.Direct3D11.RenderTargetViewDescription()
                {
                    Dimension = SharpDX.Direct3D11.RenderTargetViewDimension.Texture2D,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb,
                };
                var render_target = new SharpDX.Direct3D11.RenderTargetView(baseDevice.NativeDevice, texture, target_desc);
                render_targets[i] = render_target;
            }
#endif

            // Do not allocate these every frame to save some resources
            views = new View[view_count]; //(XrView*)malloc(sizeof(XrView) * view_count);
            for (int i = 0; i < view_count; i++)
            {
                views[i].Type = StructureType.TypeView;
                views[i].Next = null;
            }

            projection_views = new CompositionLayerProjectionView[view_count]; //(XrCompositionLayerProjectionView*)malloc(sizeof(XrCompositionLayerProjectionView) * view_count);
            for (int i = 0; i < view_count; i++)
            {
                projection_views[i].Type = StructureType.TypeCompositionLayerProjectionView; //XR_TYPE_COMPOSITION_LAYER_PROJECTION_VIEW;
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
                Type = StructureType.TypeReferenceSpaceCreateInfo,
                Next = null,
                ReferenceSpaceType = play_space_type,
                PoseInReferenceSpace = new Posef(new Quaternionf(0f, 0f, 0f, 1f), new Vector3f(0f, 0f, 0f)),
            };
            var play_space = new Space();
            CheckResult(Xr.CreateReferenceSpace(session, &play_space_create_info, &play_space), "CreateReferenceSpace");
            globalPlaySpace = play_space;

            ActionSetCreateInfo gameplay_actionset_info = new ActionSetCreateInfo()
            {
                Type = StructureType.TypeActionSetCreateInfo,
                Next = null,
            };

            Span<byte> asname = new Span<byte>(gameplay_actionset_info.ActionSetName, 16);
            Span<byte> lsname = new Span<byte>(gameplay_actionset_info.LocalizedActionSetName, 16);
            SilkMarshal.StringIntoSpan("actionset\0", asname);
            SilkMarshal.StringIntoSpan("ActionSet\0", lsname);

            ActionSet gameplay_actionset;
            CheckResult(Xr.CreateActionSet(Instance, &gameplay_actionset_info, &gameplay_actionset), "CreateActionSet");
            globalActionSet = gameplay_actionset;

            OpenXRInput.Initialize(this);

            leftHand = new OpenXrTouchController(this, TouchControllerHand.Left);
            rightHand = new OpenXrTouchController(this, TouchControllerHand.Right);

            SessionActionSetsAttachInfo actionset_attach_info = new SessionActionSetsAttachInfo()
            {
                Type = StructureType.TypeSessionActionSetsAttachInfo,
                Next = null,
                CountActionSets = 1,
                ActionSets = &gameplay_actionset
            };

            CheckResult(Xr.AttachSessionActionSets(session, &actionset_attach_info), "AttachSessionActionSets");

            // figure out what interaction profile we are using, and determine if it has a touchpad/thumbstick or both
            handProfileState = new InteractionProfileState()
            {
                Type = StructureType.TypeInteractionProfileState,
                Next = null,
            };
            Xr.StringToPath(Instance, "/user/hand/left", ref leftHandPath);
        }

        private void EndNullFrame()
        {
            FrameEndInfo frame_end_info = new FrameEndInfo()
            {
                Type = StructureType.TypeFrameEndInfo,
                DisplayTime = globalFrameState.PredictedDisplayTime,
                EnvironmentBlendMode = EnvironmentBlendMode.Opaque,
                LayerCount = 0,
                Layers = null,
                Next = null,
            };
            CheckResult(Xr.EndFrame(globalSession, frame_end_info), "BeginFrame");
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
                Type = StructureType.TypeFrameWaitInfo,
            };

            //Logger.Warning("WaitFrame");
            globalFrameState = new FrameState()
            {
                Type = StructureType.TypeFrameState,
                Next = null,
            };
            CheckResult(Xr.WaitFrame(globalSession, in frame_wait_info, ref globalFrameState), "WaitFrame");

            FrameBeginInfo frame_begin_info = new FrameBeginInfo()
            {
                Type = StructureType.TypeFrameBeginInfo,
                Next = null,
            };

            //Logger.Warning("BeginFrame");
            CheckResult(Xr.BeginFrame(globalSession, frame_begin_info), "BeginFrame");

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
            // --- Create projection matrices and view matrices for each eye
            ViewLocateInfo view_locate_info = new ViewLocateInfo()
            {
                Type = StructureType.TypeViewLocateInfo,
                ViewConfigurationType = ViewConfigurationType.PrimaryStereo,
                DisplayTime = globalFrameState.PredictedDisplayTime,
                Space = globalPlaySpace,
                Next = null,
            };

            ViewState view_state = new ViewState()
            {
                Type = StructureType.TypeViewState,
                Next = null,
            };

            unsafe
            {
                uint view_count = 2;
                // Logger.Warning("LocateView");
                CheckResult(Xr.LocateView(globalSession, &view_locate_info, &view_state, 2, &view_count, views), "XrLocateView");
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
            var acquireInfo = new SwapchainImageAcquireInfo() { 
                Type = StructureType.TypeSwapchainImageAcquireInfo,
                Next = null,
            };
            CheckResult(Xr.AcquireSwapchainImage(globalSwapchain, in acquireInfo, ref swapchainIndex), "AcquireSwapchainImage");

            // Logger.Warning("WaitSwapchainImage");
            var waitInfo = new SwapchainImageWaitInfo(timeout: long.MaxValue) { 
                Type = StructureType.TypeSwapchainImageWaitInfo,
                Next = null,
            };
            swapImageCollected = (Xr.WaitSwapchainImage(globalSwapchain, in waitInfo) == Result.Success);
            if(!swapImageCollected)
            {
                // Logger.Warning("WaitSwapchainImage failed");
                var releaseInfo = new SwapchainImageReleaseInfo() { 
                    Type = StructureType.TypeSwapchainImageReleaseInfo,
                    Next = null,
                };
                CheckResult(Xr.ReleaseSwapchainImage(globalSwapchain, in releaseInfo), "ReleaseSwapchainImage");
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
            // if we didn't wait a frame, don't commit
            if (begunFrame == false)
                return;

            begunFrame = false;

            if (swapImageCollected)
            {
#if STRIDE_GRAPHICS_API_DIRECT3D11
            Debug.Assert(commandList.NativeDeviceContext == baseDevice.NativeDeviceContext);
            // Logger.Warning("Blit render target");
            baseDevice.NativeDeviceContext.CopyResource(renderFrame.NativeRenderTargetView.Resource, render_targets[swapchainPointer].Resource);
#endif

            // Release the swapchain image
            // Logger.Warning("ReleaseSwapchainImage");
            var releaseInfo = new SwapchainImageReleaseInfo() { 
                Type = StructureType.TypeSwapchainImageReleaseInfo,
                Next = null,
            };
            CheckResult(Xr.ReleaseSwapchainImage(globalSwapchain, in releaseInfo), "ReleaseSwapchainImage");

                for (var eye = 0; eye < 2; eye++)
                {
                    projection_views[eye].Fov = views[eye].Fov;
                    projection_views[eye].Pose = views[eye].Pose;
                }

                unsafe
                {
                fixed (CompositionLayerProjectionView* projection_views_ptr = &projection_views[0])
                {
                    var projectionLayer = new CompositionLayerProjection
                    (
                        viewCount: (uint)projection_views.Length,
                        views: projection_views_ptr,
                        space: globalPlaySpace
                    );

                    var layerPointer = (CompositionLayerBaseHeader*)&projectionLayer;
                    var frameEndInfo = new FrameEndInfo()
                    {
                        Type = StructureType.TypeFrameEndInfo,
                        DisplayTime = globalFrameState.PredictedDisplayTime,
                        EnvironmentBlendMode = EnvironmentBlendMode.Opaque,
                        LayerCount = 1,
                        Layers = &layerPointer,
                        Next = null,
                    };

                    //Logger.Warning("EndFrame");
                    CheckResult(Xr.EndFrame(globalSession, in frameEndInfo), "EndFrame");
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
                CheckResult(Xr.DestroySpace(globalPlaySpace), "DestroySpace");
            }

            var play_space = new Space();
            ReferenceSpaceCreateInfo play_space_create_info = new ReferenceSpaceCreateInfo()
            {
                Type = StructureType.TypeReferenceSpaceCreateInfo,
                Next = null,
                ReferenceSpaceType = play_space_type,
                PoseInReferenceSpace = new Posef(new Quaternionf(0f, 0f, 0f, 1f), new Vector3f(0f, 0f, 0f)),
            };
            unsafe
            {
                CheckResult(Xr.CreateReferenceSpace(globalSession, &play_space_create_info, &play_space), "CreateReferenceSpace");
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

#if DEBUG_OPENXR
        private static unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT types, DebugUtilsMessengerCallbackDataEXT* msg, void* user_data)
        {
            // Print the debug message we got! There's a bunch more info we could
            // add here too, but this is a pretty good start, and you can always
            // add a breakpoint this line!
            var function_name = Marshal.PtrToStringAnsi(new System.IntPtr(msg->FunctionName));
            var message = Marshal.PtrToStringAnsi(new System.IntPtr(msg->Message));
            Logger.Warning(function_name + " " + message);

            // Returning XR_TRUE here will force the calling function to fail
            return 0;
        }
#endif

        private unsafe void PrintApiLayers()
        {
            uint count = 0;
            CheckResult(Xr.EnumerateApiLayerProperties(0, &count, null), "EnumerateApiLayerProperties");

            if (count == 0)
            {

                Logger.Debug("No API Layers");
                return;
            }

            var props = new ApiLayerProperties[count];
            for (uint i = 0; i < count; i++)
            {
                props[i].Type = StructureType.TypeApiLayerProperties;
                props[i].Next = null;
            }

            CheckResult(Xr.EnumerateApiLayerProperties(count, &count, props), "EnumerateApiLayerProperties");

            Logger.Debug("API Layers:");
            for (uint i = 0; i < count; i++)
            {
                fixed (void* nptr = props[i].LayerName)
                fixed (void* dptr = props[i].Description)
                    Logger.Debug(
                        Marshal.PtrToStringAnsi(new System.IntPtr(nptr))
                        + " "
                        + props[i].LayerVersion
                        + " "
                        + Marshal.PtrToStringAnsi(new System.IntPtr(dptr))
                    );
            }
        }

        private unsafe void PrintSystemProperties(SystemProperties system_properties)
        {
            Logger.Debug(
                "System properties: "
                + Marshal.PtrToStringAnsi(new System.IntPtr(system_properties.SystemName))
                + ", vendor: "
                + Marshal.PtrToStringAnsi(new System.IntPtr(system_properties.VendorId))
            );
            Logger.Debug(
                "Max layers: "
                + system_properties.GraphicsProperties.MaxLayerCount
            );
            Logger.Debug(
                "Max swapchain size: "
                + system_properties.GraphicsProperties.MaxSwapchainImageWidth
                + "x"
                + system_properties.GraphicsProperties.MaxSwapchainImageHeight
            );
            Logger.Debug(
                "Orientation Tracking: "
                + system_properties.TrackingProperties.OrientationTracking
            );
            Logger.Debug(
                "tPosition Tracking: "
                + system_properties.TrackingProperties.PositionTracking
            );
        }

        private unsafe void PrintViewConfigViews(ViewConfigurationView[] viewconfig_views)
        {
            foreach (var viewconfig_view in viewconfig_views)
            {
                Logger.Debug("View Configuration View:");
                Logger.Debug(
                    "Resolution: Recommended "
                    + viewconfig_view.RecommendedImageRectWidth + "x" + viewconfig_view.RecommendedImageRectHeight
                    + " Max: " + viewconfig_view.MaxImageRectWidth + "x" + viewconfig_view.MaxImageRectHeight
                );
                Logger.Debug(
                    "Swapchain Samples: Recommended"
                    + viewconfig_view.RecommendedSwapchainSampleCount
                    + " Max: " + viewconfig_view.MaxSwapchainSampleCount
                );
            }
        }

        private void ReleaseUnmanagedResources()
        {
            if (_unmanagedResourcesFreed)
            {
                return;
            }

            CheckResult(Xr.DestroyInstance(Instance), "DestroyInstance");
            _unmanagedResourcesFreed = true;
        }

        internal Matrix createViewMatrix(Vector3 translation, Quaternion rotation)
        {
            Matrix rotationMatrix = Matrix.RotationQuaternion(rotation);
            Matrix translationMatrix = Matrix.Translation(translation);
            Matrix viewMatrix = translationMatrix * rotationMatrix;
            viewMatrix.Invert();
            return viewMatrix;
        }

        internal Matrix createProjectionFov(Fovf fov, float nearZ, float farZ)
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
        internal static Quaternion ConvertToFocus(ref Quaternionf quat)
        {
            return new Quaternion(-quat.X, -quat.Y, -quat.Z, quat.W);
        }

        public override void ReadEyeParameters(Eyes eye, float near, float far, ref Vector3 cameraPosition, ref Matrix cameraRotation, bool ignoreHeadRotation, bool ignoreHeadPosition, out Matrix view, out Matrix projection)
        {
            Matrix eyeMat, rot;
            Vector3 pos, scale;

            View eyeview = views[(int)eye];

            projection = createProjectionFov(eyeview.Fov, near, far);
            var adjustedHeadMatrix = createViewMatrix(new Vector3(-eyeview.Pose.Position.X, -eyeview.Pose.Position.Y, -eyeview.Pose.Position.Z),
                                                      ConvertToFocus(ref eyeview.Pose.Orientation));
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
                Type = StructureType.TypeEventDataBuffer,
                Next = null
            };
            while (Xr.PollEvent(Instance, &runtime_event) == Result.Success)
            {
                switch (runtime_event.Type)
                {
                    case StructureType.TypeEventDataInstanceLossPending:
                        {
                            var loss_event = Unsafe.As<EventDataBuffer, EventDataInstanceLossPending>(ref runtime_event);
                            Logger.Warning("EVENT: instance loss pending at " + loss_event.LossTime + ". Destroying instance.");
                            break;
                        }
                    case StructureType.TypeEventDataSessionStateChanged:
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
                                                Type = StructureType.TypeSessionBeginInfo,
                                                Next = null,
                                                PrimaryViewConfigurationType = ViewConfigurationType.PrimaryStereo,
                                            };
                                            CheckResult(Xr.BeginSession(globalSession, &session_begin_info), "XrBeginSession");
                                            sessionRunning = true;
                                        }
                                        runFramecycle = true;
                                        break;
                                    }
                                case SessionState.Stopping:
                                    {
                                        if(sessionRunning)
                                        {
                                            CheckResult(Xr.EndSession(globalSession), "XrEndSession");
                                            sessionRunning = false;
                                        }
                                        runFramecycle = false;
                                        break;
                                    }
                                case SessionState.LossPending:
                                case SessionState.Exiting:
                                    {
                                        CheckResult(Xr.DestroySession(globalSession), "XrDestroySession");
                                        runFramecycle = false;
                                        break;
                                    }
                            }
                            break;
                        }
                    case StructureType.TypeEventDataInteractionProfileChanged:
                        {
                            Logger.Debug("EVENT: interaction profile changed");
                            var profile_changed_event = Unsafe.As<EventDataBuffer, EventDataInteractionProfileChanged>(ref runtime_event);
                            CheckResult(Xr.GetCurrentInteractionProfile(profile_changed_event.Session, leftHandPath, ref handProfileState), "GetCurrentInteractionProfile");
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
                runtime_event.Type = StructureType.TypeEventDataBuffer;
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
                Type = StructureType.TypeActionsSyncInfo,
                Next = null,
                CountActiveActionSets = 1,
                ActiveActionSets = &active_actionsets,
            };

            Xr.SyncAction(globalSession, &actions_sync_info);

            leftHand.Update(gameTime);
            rightHand.Update(gameTime);
        }

        public override void Dispose()
        {
#if STRIDE_GRAPHICS_API_DIRECT3D11
            foreach (var render_target in render_targets)
            {
                render_target.Dispose();
            }
#endif

            CheckResult(Xr.DestroySpace(globalPlaySpace), "DestroySpace");
            CheckResult(Xr.DestroyActionSet(globalActionSet), "DestroyActionSet");
            CheckResult(Xr.DestroySwapchain(globalSwapchain), "DestroySwapchain");
            CheckResult(Xr.DestroySession(globalSession), "DestroySession");
            CheckResult(Xr.DestroyInstance(Instance), "DestroyInstance");
        }
    }
}
