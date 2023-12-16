using Silk.NET.OpenXR;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.VirtualReality
{
    public class EXT_FB_passthrough
    {
        private unsafe delegate Result pfnxrCreatePassthroughFB(Session session, PassthroughCreateInfoFB* createInfo, PassthroughFB* outPassthrough);
        private unsafe delegate Result pfnxrCreatePassthroughLayerFB(Session session, PassthroughLayerCreateInfoFB* createInfo, PassthroughLayerFB* outPassthroughLayer);
        private unsafe delegate Result pfnxrDestroyPassthroughFB(PassthroughFB outPassthrough);
        private unsafe delegate Result pfnxrDestroyPassthroughLayerFB(PassthroughLayerFB outPassthroughLayer);
        private unsafe delegate Result pfnxrPassthroughStartFB(PassthroughFB outPassthrough);
        private unsafe delegate Result pfnxrPassthroughPauseFB(PassthroughFB outPassthrough);
        private unsafe delegate Result pfnxrPassthroughLayerResumeFB(PassthroughLayerFB layer);
        private unsafe delegate Result pfnxrPassthroughLayerPauseFB(PassthroughLayerFB layer);

        private pfnxrCreatePassthroughLayerFB createPassthroughLayerFB;
        private pfnxrDestroyPassthroughFB destroyPassthroughFB;
        private pfnxrDestroyPassthroughLayerFB destroyPassthroughLayerFB;
        private pfnxrPassthroughStartFB passthroughStartFB;
        private pfnxrPassthroughPauseFB passthroughPauseFB;
        private pfnxrPassthroughLayerResumeFB passthroughLayerResumeFB;
        private pfnxrPassthroughLayerPauseFB passthroughLayerPauseFB;

        PassthroughFB passthrough_Handle;
        PassthroughLayerFB passthrough_Layer;
        IntPtr activeCompositionlayer;

        Session session;

        internal unsafe void Initialize(XR xr, Session session, Instance instance)
        {
            this.session = session;

            Silk.NET.Core.PfnVoidFunction xrCreatePassthroughFB = new Silk.NET.Core.PfnVoidFunction();
            OpenXRHmd.CheckResult(xr.GetInstanceProcAddr(instance, "xrCreatePassthroughFB", ref xrCreatePassthroughFB), "GetinstanceProcAddr::xrCreatePassthroughFB");
            var createPassthroughFB = (pfnxrCreatePassthroughFB)Marshal.GetDelegateForFunctionPointer((IntPtr)xrCreatePassthroughFB.Handle, typeof(pfnxrCreatePassthroughFB));

            Silk.NET.Core.PfnVoidFunction xrDestroyPassthroughFB = new Silk.NET.Core.PfnVoidFunction();
            OpenXRHmd.CheckResult(xr.GetInstanceProcAddr(instance, "xrDestroyPassthroughFB", ref xrDestroyPassthroughFB), "GetinstanceProcAddr::xrDestroyPassthroughFB");
            destroyPassthroughFB = (pfnxrDestroyPassthroughFB)Marshal.GetDelegateForFunctionPointer((IntPtr)xrDestroyPassthroughFB.Handle, typeof(pfnxrDestroyPassthroughFB));

            Silk.NET.Core.PfnVoidFunction xrPassthroughStartFB = new Silk.NET.Core.PfnVoidFunction();
            OpenXRHmd.CheckResult(xr.GetInstanceProcAddr(instance, "xrPassthroughStartFB", ref xrPassthroughStartFB), "GetinstanceProcAddr::xrPassthroughStartFB");
            passthroughStartFB = (pfnxrPassthroughStartFB)Marshal.GetDelegateForFunctionPointer((IntPtr)xrPassthroughStartFB.Handle, typeof(pfnxrPassthroughStartFB));

            Silk.NET.Core.PfnVoidFunction xrPassthroughPauseFB = new Silk.NET.Core.PfnVoidFunction();
            OpenXRHmd.CheckResult(xr.GetInstanceProcAddr(instance, "xrPassthroughPauseFB", ref xrPassthroughPauseFB), "GetinstanceProcAddr::xrPassthroughPauseFB");
            passthroughPauseFB = (pfnxrPassthroughPauseFB)Marshal.GetDelegateForFunctionPointer((IntPtr)xrPassthroughPauseFB.Handle, typeof(pfnxrPassthroughPauseFB));

            Silk.NET.Core.PfnVoidFunction xrCreatePassthroughLayerFB = new Silk.NET.Core.PfnVoidFunction();
            OpenXRHmd.CheckResult(xr.GetInstanceProcAddr(instance, "xrCreatePassthroughLayerFB", ref xrCreatePassthroughLayerFB), "GetinstanceProcAddr::xrCreatePassthroughLayerFB");
            createPassthroughLayerFB = (pfnxrCreatePassthroughLayerFB)Marshal.GetDelegateForFunctionPointer((IntPtr)xrCreatePassthroughLayerFB.Handle, typeof(pfnxrCreatePassthroughLayerFB));

            Silk.NET.Core.PfnVoidFunction xrDestroyPassthroughLayerFB = new Silk.NET.Core.PfnVoidFunction();
            OpenXRHmd.CheckResult(xr.GetInstanceProcAddr(instance, "xrDestroyPassthroughLayerFB", ref xrDestroyPassthroughLayerFB), "GetinstanceProcAddr::xrDestroyPassthroughLayerFB");
            destroyPassthroughLayerFB = (pfnxrDestroyPassthroughLayerFB)Marshal.GetDelegateForFunctionPointer((IntPtr)xrDestroyPassthroughLayerFB.Handle, typeof(pfnxrDestroyPassthroughLayerFB));

            Silk.NET.Core.PfnVoidFunction xrPassthroughLayerResumeFB = new Silk.NET.Core.PfnVoidFunction();
            OpenXRHmd.CheckResult(xr.GetInstanceProcAddr(instance, "xrPassthroughLayerResumeFB", ref xrPassthroughLayerResumeFB), "GetinstanceProcAddr::xrPassthroughLayerResumeFB");
            passthroughLayerResumeFB = (pfnxrPassthroughLayerResumeFB)Marshal.GetDelegateForFunctionPointer((IntPtr)xrPassthroughLayerResumeFB.Handle, typeof(pfnxrPassthroughLayerResumeFB));

            Silk.NET.Core.PfnVoidFunction xrPassthroughLayerPauseFB = new Silk.NET.Core.PfnVoidFunction();
            OpenXRHmd.CheckResult(xr.GetInstanceProcAddr(instance, "xrPassthroughLayerPauseFB", ref xrPassthroughLayerPauseFB), "GetinstanceProcAddr::xrPassthroughLayerPauseFB");
            passthroughLayerPauseFB = (pfnxrPassthroughLayerPauseFB)Marshal.GetDelegateForFunctionPointer((IntPtr)xrPassthroughLayerPauseFB.Handle, typeof(pfnxrPassthroughLayerPauseFB));

            var passthroughCreateInfo = new PassthroughCreateInfoFB
            {
                Next = null,
                Flags = 0,
                Type = StructureType.TypePassthroughCreateInfoFB
            };

            var passthrough = new PassthroughFB();
            OpenXRHmd.CheckResult(createPassthroughFB(session, &passthroughCreateInfo, &passthrough), "XrCreatePassthroughFB");
            passthrough_Handle = passthrough;
            
            this.activeCompositionlayer = Marshal.AllocHGlobal(sizeof(CompositionLayerPassthroughFB));
            Unsafe.InitBlockUnaligned((byte*)this.activeCompositionlayer, 0, (uint)sizeof(CompositionLayerPassthroughFB));
        }

        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set 
            {
                if (enabled != value) 
                { 
                    if (!enabled)
                    {
                        //start the extension
                        OpenXRHmd.CheckResult(passthroughStartFB(passthrough_Handle), "XrPassthroughStartFB");

                        //create the layer
                        var passthroughLayerCreateInfo = new PassthroughLayerCreateInfoFB
                        {
                            Next = null,
                            Flags = PassthroughFlagsFB.PassthroughIsRunningATCreationBitFB,
                            Passthrough = passthrough_Handle,
                            Purpose = PassthroughLayerPurposeFB.PassthroughLayerPurposeReconstructionFB,
                            Type = StructureType.TypePassthroughLayerCreateInfoFB
                        };

                        unsafe
                        {
                            var passthroughLayer = new PassthroughLayerFB();
                            OpenXRHmd.CheckResult(createPassthroughLayerFB(session, &passthroughLayerCreateInfo, &passthroughLayer), "xrCreatePassthroughLayerFB");
                            passthrough_Layer = passthroughLayer;
                        }

                        enabled = true;
                    }
                    else
                    {
                        if (!passthrough_Layer.Equals(default(PassthroughLayerFB)))
                            destroyPassthroughLayerFB(passthrough_Layer);

                        if (!passthrough_Handle.Equals(default(PassthroughFB)))
                            passthroughPauseFB(passthrough_Handle);

                        enabled = false;
                    }
                }
            }
        }

        internal unsafe IntPtr GetCompositionLayer()
        {
            var activeCompositionlayer = (CompositionLayerPassthroughFB*)this.activeCompositionlayer;
            activeCompositionlayer->Next = null;
            activeCompositionlayer->Flags = CompositionLayerFlags.CompositionLayerBlendTextureSourceAlphaBit;
            activeCompositionlayer->LayerHandle = passthrough_Layer;
            activeCompositionlayer->Type = StructureType.TypeCompositionLayerPassthroughFB;
            return this.activeCompositionlayer;
        }

        internal unsafe void Destroy()
        {
            Enabled = false;
            destroyPassthroughFB(passthrough_Handle);
            if (activeCompositionlayer != null)
                Marshal.FreeHGlobal(activeCompositionlayer);
        }
    }
}
