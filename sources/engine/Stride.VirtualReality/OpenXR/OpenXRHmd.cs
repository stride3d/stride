using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Silk.NET.OpenXR;
using System.Runtime.InteropServices;
using System.Linq;
using Silk.NET.Core;
using System.Diagnostics;
using Silk.NET.Core.Native;
using Stride.Graphics.SDL;
using Vortice.Vulkan;

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
        public ReferenceSpaceType play_space_type = ReferenceSpaceType.Stage; //XR_REFERENCE_SPACE_TYPE_LOCAL;
        //public SwapchainImageVulkan2KHR[] images;
        //public SwapchainImageVulkan2KHR[] depth_images;
        public SwapchainImageVulkanKHR[] images;
        public SwapchainImageVulkanKHR[] depth_images;
        public ActionSet globalActionSet;

        // array of view_count containers for submitting swapchains with rendered VR frames
        CompositionLayerProjectionView[] projection_views;

        // array of view_count views, filled by the runtime with current HMD display pose
        View[] views;

        // ExtDebugUtils is a handy OpenXR debugging extension which we'll enable if available unless told otherwise.
        public bool? IsDebugUtilsSupported;

        // OpenXR handles
        public Instance Instance;
        public ulong system_id = 0;

        // Misc
        private bool _unmanagedResourcesFreed;

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
                throw new InvalidOperationException($"OpenXR error! Make sure a Vulkan-enabled OpenXR runtime is set & running (like SteamVR)\n\nCode: {result} ({result:X}) in " + forFunction + "\n\nStack Trace: " + (new StackTrace()).ToString());

            return result;
        }

        private List<string> Extensions = new List<string>();

        public unsafe ulong GetSwapchainImage()
        {
            // Get the swapchain image
            var swapchainIndex = 0u;
            var acquireInfo = new SwapchainImageAcquireInfo() { Type = StructureType.TypeSwapchainImageAcquireInfo };
            CheckResult(Xr.AcquireSwapchainImage(globalSwapchain, in acquireInfo, ref swapchainIndex), "AcquireSwapchainImage");

            var waitInfo = new SwapchainImageWaitInfo(timeout: long.MaxValue) { Type = StructureType.TypeSwapchainImageWaitInfo };
            swapImageCollected = Xr.WaitSwapchainImage(globalSwapchain, in waitInfo) == Result.Success;

            return images[swapchainIndex].Image;
        }

        private unsafe void Prepare()
        {
            // Create our API object for OpenXR.
            Xr = XR.GetApi();

            Extensions.Clear();
            //Extensions.Add("XR_KHR_vulkan_enable2");
            Extensions.Add("XR_KHR_vulkan_enable");
            Extensions.Add("XR_EXT_hp_mixed_reality_controller");
            Extensions.Add("XR_HTC_vive_cosmos_controller_interaction");
            Extensions.Add("XR_MSFT_hand_interaction");
            Extensions.Add("XR_EXT_samsung_odyssey_controller");

            uint propCount = 0;
            Xr.EnumerateInstanceExtensionProperties((byte*)null, 0, &propCount, null);

            ExtensionProperties[] props = new ExtensionProperties[propCount];
            for (int i = 0; i < props.Length; i++) props[i].Type = StructureType.TypeExtensionProperties;

            fixed (ExtensionProperties* pptr = &props[0])
                Xr.EnumerateInstanceExtensionProperties((byte*)null, propCount, ref propCount, pptr);

            List<string> AvailableExtensions = new List<string>();
            for (int i = 0; i < props.Length; i++)
            {
                fixed (void* nptr = props[i].ExtensionName)
                    AvailableExtensions.Add(Marshal.PtrToStringAnsi(new System.IntPtr(nptr)));
            }

            for (int i=0; i<Extensions.Count; i++)
            {
                if (AvailableExtensions.Contains(Extensions[i]) == false)
                {
                    Extensions.RemoveAt(i);
                    i--;
                }
            }

            InstanceCreateInfo instanceCreateInfo;

            var appInfo = new ApplicationInfo()
            {
                ApiVersion = new Version64(1, 0, 9)
            };

            // We've got to marshal our strings and put them into global, immovable memory. To do that, we use
            // SilkMarshal.
            Span<byte> appName = new Span<byte>(appInfo.ApplicationName, 128);
            Span<byte> engName = new Span<byte>(appInfo.EngineName, 128);
            SilkMarshal.StringIntoSpan(System.AppDomain.CurrentDomain.FriendlyName, appName);
            SilkMarshal.StringIntoSpan("FocusEngine", engName);

            var requestedExtensions = SilkMarshal.StringArrayToPtr(Extensions);
            instanceCreateInfo = new InstanceCreateInfo
            (
                applicationInfo: appInfo,
                enabledExtensionCount: (uint)Extensions.Count,
                enabledExtensionNames: (byte**)requestedExtensions
            );

            // Now we're ready to make our instance!
            CheckResult(Xr.CreateInstance(in instanceCreateInfo, ref Instance), "CreateInstance");

            // For our benefit, let's log some information about the instance we've just created.
            InstanceProperties properties = new();
            CheckResult(Xr.GetInstanceProperties(Instance, ref properties), "GetInstanceProperties");

            var runtimeName = SilkMarshal.PtrToString((nint)properties.RuntimeName);
            var runtimeVersion = ((Version)(Version64)properties.RuntimeVersion).ToString(3);

            Console.WriteLine($"[INFO] Application: Using OpenXR Runtime \"{runtimeName}\" v{runtimeVersion}");

            // We're creating a head-mounted-display (HMD, i.e. a VR headset) example, so we ask for a runtime which
            // supports that form factor. The response we get is a ulong that is the System ID.
            var getInfo = new SystemGetInfo(formFactor: FormFactor.HeadMountedDisplay);
            CheckResult(Xr.GetSystem(Instance, in getInfo, ref system_id), "GetSystem");
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

        private GraphicsDevice baseDevice;

        private Size2 renderSize;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate Result pfnGetVulkanGraphicsRequirements2KHR(Instance instance, ulong sys_id, GraphicsRequirementsVulkanKHR* req);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate Result pfnGetVulkanGraphicsDevice2KHR(Instance instance, VulkanGraphicsDeviceGetInfoKHR* getInfo, VkPhysicalDevice* vulkanPhysicalDevice);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate Result pfnGetVulkanGraphicsRequirementsKHR(Instance instance, ulong sys_id, GraphicsRequirementsVulkanKHR* req);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate Result pfnGetVulkanGraphicsDeviceKHR(Instance instance, ulong systemId, VkHandle vkInstance, VkHandle* vkPhysicalDevice);

        public OpenXRHmd(GraphicsDevice gd)
        {
            baseDevice = gd;
            VRApi = VRApi.OpenXR;
        }

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

        //private ulong poseCount;
        //public override ulong PoseCount => poseCount;

        public override bool CanInitialize => true;

        public override Size2 OptimalRenderFrameSize => renderSize;

        // TODO (not implemented)
        private Texture mirrorTexture;
        public override Texture MirrorTexture { get => mirrorTexture; protected set => mirrorTexture = value; }

        public override TrackedItem[] TrackedItems => null;

        internal Texture swapTexture;
        internal bool begunFrame, swapImageCollected;
        internal ulong swapchainPointer;

        public override unsafe void Commit(CommandList commandList, Texture renderFrame)
        {
            // if we didn't wait a frame, don't commit
            if (begunFrame == false || swapImageCollected == false)
                return;

#if STRIDE_GRAPHICS_API_VULKAN
            // copy texture to swapchain image
            swapTexture.SetFullHandles(new VkImage(swapchainPointer), VkImageView.Null, 
                                       renderFrame.NativeLayout, renderFrame.NativeAccessMask,
                                       renderFrame.NativeFormat, renderFrame.NativeImageAspect);
#endif
            commandList.Copy(renderFrame, swapTexture);
        }

        public unsafe void Flush()
        {
            // if we didn't wait a frame, don't commit
            if (begunFrame == false)
                return;

            begunFrame = false;

            // Release the swapchain image
            var releaseInfo = new SwapchainImageReleaseInfo() { Type = StructureType.TypeSwapchainImageReleaseInfo };
            CheckResult(Xr.ReleaseSwapchainImage(globalSwapchain, in releaseInfo), "ReleaseSwapchainImage");

            // https://github.com/dotnet/Silk.NET/blob/b0b31779ce4db9b68922977fa11772b95f506e09/examples/CSharp/OpenGL%20Demos/OpenGL%20VR%20Demo/OpenXR/Renderer.cs#L507
            var frameEndInfo = new FrameEndInfo()
            {
                Type = StructureType.TypeFrameEndInfo,
                DisplayTime = globalFrameState.PredictedDisplayTime,
                EnvironmentBlendMode = EnvironmentBlendMode.Opaque
            };

            fixed (CompositionLayerProjectionView* ptr = &projection_views[0])
            {
                var projectionLayer = new CompositionLayerProjection
                (
                    viewCount: 2,
                    views: ptr,
                    space: globalPlaySpace
                );

                var layerPointer = (CompositionLayerBaseHeader*)&projectionLayer;
                for (var eye = 0; eye < 2; eye++)
                {
                    ref var layerView = ref projection_views[eye];
                    layerView.Fov = views[eye].Fov;
                    layerView.Pose = views[eye].Pose;
                }

                frameEndInfo.LayerCount = 1;
                frameEndInfo.Layers = &layerPointer;

                CheckResult(Xr.EndFrame(globalSession, in frameEndInfo), "EndFrame");
            }
        }

        internal static Quaternion ConvertToFocus(ref Quaternionf quat)
        {
            return new Quaternion(-quat.X, -quat.Y, -quat.Z, quat.W);
        }

        public override unsafe void Draw(GameTime gameTime)
        {
            // wait get poses (headPos etc.)
            // --- Wait for our turn to do head-pose dependent computation and render a frame
            FrameWaitInfo frame_wait_info = new FrameWaitInfo()
            {
                Type = StructureType.TypeFrameWaitInfo,
            };

            CheckResult(Xr.WaitFrame(globalSession, in frame_wait_info, ref globalFrameState), "WaitFrame");

            if ((Bool32)globalFrameState.ShouldRender)
            {
                FrameBeginInfo frame_begin_info = new FrameBeginInfo()
                {
                    Type = StructureType.TypeFrameBeginInfo,
                };

                CheckResult(Xr.BeginFrame(globalSession, &frame_begin_info), "BeginFrame");

                swapchainPointer = GetSwapchainImage();
                begunFrame = true;
            }
        }

        public override unsafe void Enable(GraphicsDevice device, GraphicsDeviceManager graphicsDeviceManager, bool requireMirror, int mirrorWidth, int mirrorHeight)
        {
            // Changing the form_factor may require changing the view_type too.
            ViewConfigurationType view_type = ViewConfigurationType.PrimaryStereo;

            // Typically STAGE for room scale/standing, LOCAL for seated
            Space play_space;

            // the session deals with the renderloop submitting frames to the runtime
            Session session;

            // each physical Display/Eye is described by a view.
            // view_count usually depends on the form_factor / view_type.
            // dynamically allocating all view related structs instead of assuming 2
            // hopefully allows this app to scale easily to different view_counts.
            uint view_count = 0;
            // the viewconfiguration views contain information like resolution about each view
            ViewConfigurationView[] viewconfig_views;

            // array of view_count handles for swapchains.
            // it is possible to use imageRect to render all views to different areas of the
            // same texture, but in this example we use one swapchain per view
            Swapchain swapchain;
            // array of view_count ints, storing the length of swapchains
            uint[] swapchain_lengths;

            // depth swapchain equivalent to the VR color swapchains (not supported yet)
            //Swapchain depth_swapchains;
            //uint[] depth_swapchain_lengths;

            Prepare();

            SystemProperties system_props = new SystemProperties() {
                Type = StructureType.TypeSystemProperties,
            };

            CheckResult(Xr.GetSystemProperties(Instance, system_id, &system_props), "GetSystemProperties");

            ViewConfigurationView vcv = new ViewConfigurationView()
            {
                Type = StructureType.TypeViewConfigurationView,
            };

            viewconfig_views = new ViewConfigurationView[128];
            fixed (ViewConfigurationView* viewspnt = &viewconfig_views[0])
                CheckResult(Xr.EnumerateViewConfigurationView(Instance, system_id, view_type, (uint)viewconfig_views.Length, ref view_count, viewspnt), "EnumerateViewConfigurationView");
            Array.Resize<ViewConfigurationView>(ref viewconfig_views, (int)view_count);

            // get size
            renderSize.Height = (int)Math.Round(viewconfig_views[0].RecommendedImageRectHeight * RenderFrameScaling);
            renderSize.Width = (int)Math.Round(viewconfig_views[0].RecommendedImageRectWidth * RenderFrameScaling) * 2; // 2 views in one frame

            GraphicsRequirementsVulkanKHR vulk = new GraphicsRequirementsVulkanKHR()
            {
                Type = StructureType.TypeGraphicsRequirementsVulkanKhr
            };

            Silk.NET.Core.PfnVoidFunction func = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(Xr.GetInstanceProcAddr(Instance, "xrGetVulkanGraphicsRequirementsKHR", ref func), "GetInstanceProcAddr::xrGetVulkanGraphicsRequirementsKHR");
            // this function pointer was loaded with xrGetInstanceProcAddr
            Delegate vulk_req = Marshal.GetDelegateForFunctionPointer((IntPtr)func.Handle, typeof(pfnGetVulkanGraphicsRequirementsKHR));
            vulk_req.DynamicInvoke(Instance, system_id, new System.IntPtr(&vulk));

#if STRIDE_GRAPHICS_API_VULKAN
            VkHandle physicalDevice = new VkHandle();

            // vulkan below
            CheckResult(Xr.GetInstanceProcAddr(Instance, "xrGetVulkanGraphicsDeviceKHR", ref func), "GetInstanceProcAddr::xrGetVulkanGraphicsDeviceKHR");
            Delegate vulk_dev = Marshal.GetDelegateForFunctionPointer((IntPtr)func.Handle, typeof(pfnGetVulkanGraphicsDeviceKHR));
            vulk_dev.DynamicInvoke(Instance, system_id, new VkHandle((nint)device.NativeInstance.Handle), new System.IntPtr(&physicalDevice));

            // --- Create session
            var graphics_binding_vulkan = new GraphicsBindingVulkanKHR()
            {
                Type = StructureType.TypeGraphicsBindingVulkanKhr,
                Device = new VkHandle((nint)device.NativeDevice.Handle),
                Instance = new VkHandle((nint)device.NativeInstance.Handle),
                PhysicalDevice = physicalDevice,
                QueueFamilyIndex = 0,
                QueueIndex = 0,
            };
#else
            GraphicsBindingVulkanKHR graphics_binding_vulkan = new GraphicsBindingVulkanKHR();
            throw new Exception("OpenXR is only compatible with Vulkan");
#endif

            if (graphics_binding_vulkan.PhysicalDevice.Handle == 0)
                throw new InvalidOperationException("OpenXR couldn't find a physical device.\n\nIs an OpenXR runtime running (e.g. SteamVR)?");

            SessionCreateInfo session_create_info = new SessionCreateInfo() {
                Type = StructureType.TypeSessionCreateInfo,
                Next = &graphics_binding_vulkan,
                SystemId = system_id
            };

            CheckResult(Xr.CreateSession(Instance, &session_create_info, &session), "CreateSession");
            globalSession = session;

            ReferenceSpaceCreateInfo play_space_create_info = new ReferenceSpaceCreateInfo()
            {
                Type = StructureType.TypeReferenceSpaceCreateInfo,
                ReferenceSpaceType = play_space_type,
                PoseInReferenceSpace = new Posef(new Quaternionf(0f, 0f, 0f, 1f), new Vector3f(0f, 0f, 0f))
            };

            CheckResult(Xr.CreateReferenceSpace(session, &play_space_create_info, &play_space), "CreateReferenceSpace");
            globalPlaySpace = play_space;

            // --- Create swapchain for main VR rendering
            {
                // In the frame loop we render into OpenGL textures we receive from the runtime here.
                swapchain = new Swapchain();
                swapchain_lengths = new uint[1];
                SwapchainCreateInfo swapchain_create_info = new SwapchainCreateInfo() {
                    Type = StructureType.TypeSwapchainCreateInfo,
                    UsageFlags = SwapchainUsageFlags.SwapchainUsageTransferDstBit |
                                 SwapchainUsageFlags.SwapchainUsageSampledBit |
                                 SwapchainUsageFlags.SwapchainUsageColorAttachmentBit,
                    CreateFlags = 0,
                    Format = (long)43, // VK_FORMAT_R8G8B8A8_SRGB = 43
                    SampleCount = 1, //viewconfig_views[0].RecommendedSwapchainSampleCount,
                    Width = (uint)renderSize.Width,
                    Height = (uint)renderSize.Height,
                    FaceCount = 1,
                    ArraySize = 1,
                    MipCount = 1,
                };

                CheckResult(Xr.CreateSwapchain(session, &swapchain_create_info, &swapchain), "CreateSwapchain");
                globalSwapchain = swapchain;

                swapTexture = new Texture(baseDevice, new TextureDescription()
                {
                    ArraySize = 1,
                    Depth = 1,
                    Dimension = TextureDimension.Texture2D,
                    Flags = TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                    Format = PixelFormat.R8G8B8A8_UNorm_SRgb,
                    Height = renderSize.Height,
                    MipLevels = 1,
                    MultisampleCount = MultisampleCount.None,
                    Options = TextureOptions.None,
                    Usage = GraphicsResourceUsage.Default,
                    Width = renderSize.Width,
                });

                images = new SwapchainImageVulkanKHR[32];
                uint img_count = 0;

                fixed (void* sibhp = &images[0]) {
                    CheckResult(Xr.EnumerateSwapchainImages(swapchain, (uint)images.Length, ref img_count, (SwapchainImageBaseHeader*)sibhp), "EnumerateSwapchainImages");
                }
                Array.Resize(ref images, (int)img_count);
            }

            // --- Create swapchain for depth buffers if supported (//TODO support depth buffering)
            /*{
                if (depth.supported)
                {
                    depth_swapchains = malloc(sizeof(XrSwapchain) * view_count);
                    depth_swapchain_lengths = malloc(sizeof(uint32_t) * view_count);
                    depth_images = malloc(sizeof(XrSwapchainImageOpenGLKHR*) * view_count);
                    for (uint32_t i = 0; i < view_count; i++)
                    {
                        XrSwapchainCreateInfo swapchain_create_info = {
				                .type = XR_TYPE_SWAPCHAIN_CREATE_INFO,
				                .usageFlags = XR_SWAPCHAIN_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT,
				                .createFlags = 0,
				                .format = depth_format,
				                .sampleCount = viewconfig_views[i].recommendedSwapchainSampleCount,
				                .width = viewconfig_views[i].recommendedImageRectWidth,
				                .height = viewconfig_views[i].recommendedImageRectHeight,
				                .faceCount = 1,
				                .arraySize = 1,
				                .mipCount = 1,
				                .next = NULL,
                            };

                        result = xrCreateSwapchain(session, &swapchain_create_info, &depth_swapchains[i]);
                        if (!xr_check(instance, result, "Failed to create swapchain %d!", i))
                            return 1;

                        result =
                            xrEnumerateSwapchainImages(depth_swapchains[i], 0, &depth_swapchain_lengths[i], NULL);
                        if (!xr_check(instance, result, "Failed to enumerate swapchains"))
                            return 1;

                        // these are wrappers for the actual OpenGL texture id
                        depth_images[i] = malloc(sizeof(XrSwapchainImageOpenGLKHR) * depth_swapchain_lengths[i]);
                        for (uint32_t j = 0; j < depth_swapchain_lengths[i]; j++)
                        {
                            depth_images[i][j].type = XR_TYPE_SWAPCHAIN_IMAGE_OPENGL_KHR;
                            depth_images[i][j].next = NULL;
                        }
                        result = xrEnumerateSwapchainImages(depth_swapchains[i], depth_swapchain_lengths[i],
                                                            &depth_swapchain_lengths[i],
                                                            (XrSwapchainImageBaseHeader*)depth_images[i]);
                        if (!xr_check(instance, result, "Failed to enumerate swapchain images"))
                            return 1;
                    }
                }
            }*/


            // Do not allocate these every frame to save some resources
            views = new View[view_count]; //(XrView*)malloc(sizeof(XrView) * view_count);
            for (int i = 0; i < view_count; i++)
                views[i].Type = StructureType.TypeView;

            projection_views = new CompositionLayerProjectionView[view_count]; //(XrCompositionLayerProjectionView*)malloc(sizeof(XrCompositionLayerProjectionView) * view_count);
            for (int i = 0; i < view_count; i++)
            {
                projection_views[i].Type = StructureType.TypeCompositionLayerProjectionView; //XR_TYPE_COMPOSITION_LAYER_PROJECTION_VIEW;
                projection_views[i].SubImage.Swapchain = swapchain;
                projection_views[i].SubImage.ImageArrayIndex = 0;
                projection_views[i].SubImage.ImageRect.Offset.X = (renderSize.Width * i) / 2;
                projection_views[i].SubImage.ImageRect.Offset.Y = 0;
                projection_views[i].SubImage.ImageRect.Extent.Width = renderSize.Width / 2;
                projection_views[i].SubImage.ImageRect.Extent.Height = renderSize.Height;

                // projection_views[i].{pose, fov} have to be filled every frame in frame loop
            };


            /*if (depth.supported) //TODO: depth buffering support
            {
                depth.infos = (XrCompositionLayerDepthInfoKHR*)malloc(sizeof(XrCompositionLayerDepthInfoKHR) *
                                                                      view_count);
                for (uint32_t i = 0; i < view_count; i++)
                {
                    depth.infos[i].type = XR_TYPE_COMPOSITION_LAYER_DEPTH_INFO_KHR;
                    depth.infos[i].next = NULL;
                    depth.infos[i].minDepth = 0.f;
                    depth.infos[i].maxDepth = 1.f;
                    depth.infos[i].nearZ = gl_rendering.near_z;
                    depth.infos[i].farZ = gl_rendering.far_z;

                    depth.infos[i].subImage.swapchain = depth_swapchains[i];
                    depth.infos[i].subImage.imageArrayIndex = 0;
                    depth.infos[i].subImage.imageRect.offset.x = 0;
                    depth.infos[i].subImage.imageRect.offset.y = 0;
                    depth.infos[i].subImage.imageRect.extent.width =
                        viewconfig_views[i].recommendedImageRectWidth;
                    depth.infos[i].subImage.imageRect.extent.height =
                        viewconfig_views[i].recommendedImageRectHeight;

                    // depth is chained to projection, not submitted as separate layer
                    projection_views[i].next = &depth.infos[i];
                };
            }*/

            ActionSetCreateInfo gameplay_actionset_info = new ActionSetCreateInfo()
            {
                Type = StructureType.TypeActionSetCreateInfo
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

            // --- Begin session */
            SessionBeginInfo session_begin_info = new SessionBeginInfo()
            {
                Type = StructureType.TypeSessionBeginInfo,
                PrimaryViewConfigurationType = view_type
            };

            CheckResult(Xr.BeginSession(session, &session_begin_info), "BeginSession");

            SessionActionSetsAttachInfo actionset_attach_info = new SessionActionSetsAttachInfo()
            {
	            Type = StructureType.TypeSessionActionSetsAttachInfo,
	            CountActionSets = 1,
	            ActionSets = &gameplay_actionset
            };

            CheckResult(Xr.AttachSessionActionSets(session, &actionset_attach_info), "AttachSessionActionSets");
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

	        if (farZ <= nearZ) {    
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
	        } else {
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
		        result[14] = -(farZ* (nearZ + offsetZ)) / (farZ - nearZ);

		        result[3] = 0;
		        result[7] = 0;
		        result[11] = -1;
		        result[15] = 0;
	        }

            return result;
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

            eyeMat = adjustedHeadMatrix * cameraRotation * Matrix.Translation(cameraPosition);
            eyeMat.Decompose(out scale, out rot, out pos);
            var finalUp = Vector3.TransformCoordinate(new Vector3(0, 1, 0), rot);
            var finalForward = Vector3.TransformCoordinate(new Vector3(0, 0, -1), rot);
            view = Matrix.LookAtRH(pos, pos + finalForward, finalUp);
        }

        public override unsafe void Update(GameTime gameTime)
        {
            ActiveActionSet active_actionsets = new ActiveActionSet()
            {
                ActionSet = globalActionSet
            };

            ActionsSyncInfo actions_sync_info = new ActionsSyncInfo()
            {
                Type = StructureType.TypeActionsSyncInfo,
                CountActiveActionSets = 1,
                ActiveActionSets = &active_actionsets,
            };

            Xr.SyncAction(globalSession, &actions_sync_info);

            leftHand.Update(gameTime);
            rightHand.Update(gameTime);

            // --- Create projection matrices and view matrices for each eye
            ViewLocateInfo view_locate_info = new ViewLocateInfo()
            {
                Type = StructureType.TypeViewLocateInfo,
                ViewConfigurationType = ViewConfigurationType.PrimaryStereo,
                DisplayTime = globalFrameState.PredictedDisplayTime,
                Space = globalPlaySpace
            };

            ViewState view_state = new ViewState()
            {
                Type = StructureType.TypeViewState
            };

            uint view_count;
            Xr.LocateView(globalSession, &view_locate_info, &view_state, 2, &view_count, views);

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
    }
}
