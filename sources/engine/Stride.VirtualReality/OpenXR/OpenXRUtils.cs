#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.Core;
using Silk.NET.OpenXR;
using Stride.Core.Diagnostics;

namespace Stride.VirtualReality
{
    internal static unsafe class OpenXRUtils
    {
        public const string XR_KHR_D3D11_ENABLE_EXTENSION_NAME = "XR_KHR_D3D11_enable";
        public const string XR_FB_PASSTHROUGH_EXTENSION_NAME = "XR_FB_passthrough";

        public static bool Success(this Result result) => result >= 0;

        /// <summary>
        /// A simple function which throws an exception if the given OpenXR result indicates an error has been raised.
        /// </summary>
        /// <param name="result">The OpenXR result in question.</param>
        /// <returns>
        /// The same result passed in, just in case it's meaningful and we just want to use this to filter out errors.
        /// </returns>
        /// <exception cref="OpenXRException">An exception for the given result if it indicates an error.</exception>
        [DebuggerHidden]
        [DebuggerStepThrough]
        public static Result CheckResult(this Result result, [CallerArgumentExpression(nameof(result))] string methodName = "")
        {
            if (!result.Success())
                throw new OpenXRException(result, methodName);

            return result;
        }

        public static Instance CreateRuntime(this XR xr, IReadOnlyList<string> extensions, Logger logger)
        {
            LogApiLayers(xr, logger);

            logger.Debug("Installing extensions");

            var openXrExtensions = new List<String>();
#if STRIDE_GRAPHICS_API_DIRECT3D11
            openXrExtensions.Add(XR_KHR_D3D11_ENABLE_EXTENSION_NAME);
#endif
#if DEBUG_OPENXR
            openXrExtensions.Add("XR_EXT_debug_utils");
#endif
            openXrExtensions.AddRange(extensions);

            uint propCount = 0;
            xr.EnumerateInstanceExtensionProperties((byte*)null, 0, &propCount, null);

            Span<ExtensionProperties> props = new ExtensionProperties[propCount];
            foreach (ref var prop in props)
            {
                prop.Type = StructureType.ExtensionProperties;
                prop.Next = null;
            }
            xr.EnumerateInstanceExtensionProperties((byte*)null, propCount, &propCount, props);

            logger.Debug("Supported extensions (" + propCount + "):");
            var availableExtensions = new List<string>();
            foreach (ref var prop in props)
            {
                fixed (void* nptr = prop.ExtensionName)
                {
                    var extension_name = Marshal.PtrToStringAnsi(new System.IntPtr(nptr));
                    logger.Debug(extension_name);
                    availableExtensions.Add(extension_name!);
                }
            }

            openXrExtensions.RemoveAll(e => !availableExtensions.Contains(e));

            logger.Debug("Available extensions of those enabled");
            foreach (var e in openXrExtensions)
            {
                logger.Debug(e);
            }

#if STRIDE_GRAPHICS_API_DIRECT3D11
            if (!availableExtensions.Contains("XR_KHR_D3D11_enable"))
            {
                logger.Error($"OpenXR error! Current implementation doesn't support Direct3D 11");
                return default;
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
            Instance instance = default;
            {
                var result = xr.CreateInstance(in instanceCreateInfo, ref instance);
                if (!result.Success())
                {
                    logger.Error($"Failed to create OpenXR Runtime ({result})");
                    return default;
                }
            }

#if DEBUG_OPENXR
            xr.InitializeDebugUtils(instance, logger);
#endif

            // This crashes on oculus
            // For our benefit, let's log some information about the instance we've just created.
            var properties = new InstanceProperties()
            {
                Type = StructureType.InstanceProperties,
                Next = null,
            };
            CheckResult(xr.GetInstanceProperties(instance, ref properties), "GetInstanceProperties");

            var runtimeName = Marshal.PtrToStringAnsi(new System.IntPtr(properties.RuntimeName));
            var runtimeVersion = ((Version)(Version64)properties.RuntimeVersion).ToString(3);

            logger.Info($"Using OpenXR Runtime \"{runtimeName}\" v{runtimeVersion}");

            return instance;
        }

        public static ulong GetSystem(this XR xr, Instance instance, Logger logger)
        {
            if (instance.Handle == 0)
                throw new ArgumentNullException(nameof(instance));

            // We're creating a head-mounted-display (HMD, i.e. a VR headset) example, so we ask for a runtime which
            // supports that form factor. The response we get is a ulong that is the System ID.
            var getInfo = new SystemGetInfo(formFactor: FormFactor.HeadMountedDisplay)
            {
                Type = StructureType.SystemGetInfo
            };

            ulong systemId = default;
            if (xr.GetSystem(instance, in getInfo, ref systemId).Success())
            {
                logger.Debug("Successfully got XrSystem with id " + systemId + " for HMD form factor");
                return systemId;
            }

            return default;
        }

        public static void LogApiLayers(this XR xr, Logger logger)
        {
            uint count = 0;
            CheckResult(xr.EnumerateApiLayerProperties(0, &count, null));

            if (count == 0)
            {
                logger.Debug("No API Layers");
                return;
            }

            var props = new ApiLayerProperties[count];
            for (uint i = 0; i < count; i++)
            {
                props[i].Type = StructureType.ApiLayerProperties;
                props[i].Next = null;
            }

            CheckResult(xr.EnumerateApiLayerProperties(count, &count, props));

            logger.Debug("API Layers:");
            for (uint i = 0; i < count; i++)
            {
                fixed (void* nptr = props[i].LayerName)
                fixed (void* dptr = props[i].Description)
                    logger.Debug(
                        Marshal.PtrToStringAnsi(new System.IntPtr(nptr))
                        + " "
                        + props[i].LayerVersion
                        + " "
                        + Marshal.PtrToStringAnsi(new System.IntPtr(dptr))
                    );
            }
        }

        public static void LogSystemProperties(SystemProperties systemProperties, Logger logger)
        {
            logger.Debug(
                "System properties: "
                + Marshal.PtrToStringAnsi(new System.IntPtr(systemProperties.SystemName))
                + ", vendor: "
                + Marshal.PtrToStringAnsi(new System.IntPtr(systemProperties.VendorId))
            );
            logger.Debug(
                "Max layers: "
                + systemProperties.GraphicsProperties.MaxLayerCount
            );
            logger.Debug(
                "Max swapchain size: "
                + systemProperties.GraphicsProperties.MaxSwapchainImageWidth
                + "x"
                + systemProperties.GraphicsProperties.MaxSwapchainImageHeight
            );
            logger.Debug(
                "Orientation Tracking: "
                + systemProperties.TrackingProperties.OrientationTracking
            );
            logger.Debug(
                "tPosition Tracking: "
                + systemProperties.TrackingProperties.PositionTracking
            );
        }

        public static void LogViewConfigViews(ViewConfigurationView[] viewconfig_views, Logger logger)
        {
            foreach (var viewconfig_view in viewconfig_views)
            {
                logger.Debug("View Configuration View:");
                logger.Debug(
                    "Resolution: Recommended "
                    + viewconfig_view.RecommendedImageRectWidth + "x" + viewconfig_view.RecommendedImageRectHeight
                    + " Max: " + viewconfig_view.MaxImageRectWidth + "x" + viewconfig_view.MaxImageRectHeight
                );
                logger.Debug(
                    "Swapchain Samples: Recommended"
                    + viewconfig_view.RecommendedSwapchainSampleCount
                    + " Max: " + viewconfig_view.MaxSwapchainSampleCount
                );
            }
        }

        private delegate Result pfnCreateDebugUtilsMessengerEXT(Instance instance, DebugUtilsMessengerCreateInfoEXT* createInfo, DebugUtilsMessengerEXT* messenger);
        private delegate Result pfnDestroyDebugUtilsMessengerEXT(DebugUtilsMessengerEXT messenger);

        public static void InitializeDebugUtils(this XR Xr, Instance instance, Logger logger)
        {
            Silk.NET.Core.PfnVoidFunction xrCreateDebugUtilsMessengerEXT = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(Xr.GetInstanceProcAddr(instance, "xrCreateDebugUtilsMessengerEXT", ref xrCreateDebugUtilsMessengerEXT), "GetInstanceProcAddr::xrCreateDebugUtilsMessengerEXT");
            Delegate create_debug_utils_messenger = Marshal.GetDelegateForFunctionPointer((IntPtr)xrCreateDebugUtilsMessengerEXT.Handle, typeof(pfnCreateDebugUtilsMessengerEXT));

            // https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#debug-message-categorization
            DebugUtilsMessengerCreateInfoEXT debug_info = new DebugUtilsMessengerCreateInfoEXT()
            {
                Type = StructureType.DebugUtilsMessengerCreateInfoExt,
                MessageTypes = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                    | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
                    | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt
                    | DebugUtilsMessageTypeFlagsEXT.ConformanceBitExt,
                MessageSeverities = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt
                    | DebugUtilsMessageSeverityFlagsEXT.InfoBitExt
                    | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                    | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
                UserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback,
            };

            DebugUtilsMessengerEXT xr_debug;
            var result = create_debug_utils_messenger.DynamicInvoke(instance, new System.IntPtr(&debug_info), new System.IntPtr(&xr_debug));

            uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT types, DebugUtilsMessengerCallbackDataEXT* msg, void* user_data)
            {
                // Print the debug message we got! There's a bunch more info we could
                // add here too, but this is a pretty good start, and you can always
                // add a breakpoint this line!
                var function_name = Marshal.PtrToStringAnsi(new System.IntPtr(msg->FunctionName));
                var message = Marshal.PtrToStringAnsi(new System.IntPtr(msg->Message));
                logger.Warning(function_name + " " + message);

                // Returning XR_TRUE here will force the calling function to fail
                return 0;
            }
        }
    }
}
