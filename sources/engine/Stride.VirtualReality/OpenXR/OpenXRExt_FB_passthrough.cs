using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.VirtualReality
{
    internal unsafe class OpenXRExt_FB_Passthrough
    {
        private readonly Session session;
        private readonly FBPassthrough api;
        private readonly PassthroughFB handle;
        private readonly CompositionLayerPassthroughFB* compositionlayer;

        private PassthroughLayerFB passthrough_Layer;

        public OpenXRExt_FB_Passthrough(XR xr, Session session, Instance instance)
        {
            this.session = session;

            api = new FBPassthrough(new LamdaNativeContext(TryGetProcAddress));

            var passthroughCreateInfo = new PassthroughCreateInfoFB
            {
                Next = null,
                Flags = 0,
                Type = StructureType.PassthroughCreateInfoFB
            };
            api.CreatePassthroughFB(session, passthroughCreateInfo, ref handle).CheckResult();

            this.compositionlayer = (CompositionLayerPassthroughFB*)Marshal.AllocHGlobal(sizeof(CompositionLayerPassthroughFB));
            Unsafe.InitBlockUnaligned((byte*)this.compositionlayer, 0, (uint)sizeof(CompositionLayerPassthroughFB));

            bool TryGetProcAddress(string n, out nint fptr)
            {
                PfnVoidFunction function;
                var result = xr.GetInstanceProcAddr(instance, n, ref function);
                if (result.Success())
                {
                    fptr = function;
                    return true;
                }
                else
                {
                    fptr = default;
                    return false;
                }
            }
        }

        public bool Enabled
        {
            get => passthrough_Layer.Handle != default;
            set 
            {
                if (value != Enabled) 
                { 
                    if (value)
                    {
                        //start the extension
                        api.PassthroughStartFB(handle).CheckResult();

                        //create the layer
                        var passthroughLayerCreateInfo = new PassthroughLayerCreateInfoFB
                        {
                            Next = null,
                            Flags = PassthroughFlagsFB.IsRunningATCreationBitFB,
                            Passthrough = handle,
                            Purpose = PassthroughLayerPurposeFB.ReconstructionFB,
                            Type = StructureType.PassthroughLayerCreateInfoFB
                        };

                        api.CreatePassthroughLayerFB(session, in passthroughLayerCreateInfo, ref passthrough_Layer).CheckResult();
                    }
                    else
                    {
                        api.DestroyPassthroughLayerFB(passthrough_Layer);
                        passthrough_Layer = default;

                        api.PassthroughPauseFB(handle);
                    }
                }
            }
        }

        internal unsafe CompositionLayerPassthroughFB* GetCompositionLayer()
        {
            compositionlayer->Next = null;
            compositionlayer->Flags = CompositionLayerFlags.BlendTextureSourceAlphaBit;
            compositionlayer->LayerHandle = passthrough_Layer;
            compositionlayer->Type = StructureType.CompositionLayerPassthroughFB;
            return this.compositionlayer;
        }

        internal unsafe void Destroy()
        {
            Enabled = false;
            api.DestroyPassthroughFB(handle);
            Marshal.FreeHGlobal(new nint(compositionlayer));
            api.Dispose();
        }
    }
}
