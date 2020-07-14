// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public class VoxelStorerClipmap : IVoxelStorer
    {
        public VoxelStorageClipmaps.UpdateMethods UpdatesPerFrame;

        public int storageUints;
        public Stride.Graphics.Buffer FragmentsBuffer = null;

        public int ClipMapCount;
        public int ClipMapCurrent = -1;
        public Vector3 ClipMapResolution;

        public Vector4[] PerMapOffsetScale = new Vector4[20];

        public bool UpdatesOneClipPerFrame()
        {
            return UpdatesPerFrame == VoxelStorageClipmaps.UpdateMethods.SingleClipmap || ClipMapCount <= 1;
        }


        public bool CanShareRenderStage(IVoxelStorer storer)
        {
            VoxelStorerClipmap storerClipmap = storer as VoxelStorerClipmap;
            if (storerClipmap == null)
            {
                return false;
            }

            bool singleClipA = UpdatesOneClipPerFrame();
            bool singleClipB = storerClipmap.UpdatesOneClipPerFrame();

            return singleClipA == singleClipB;
        }
        public override bool Equals(object obj)
        {
            VoxelStorerClipmap storerClipmap = obj as VoxelStorerClipmap;
            if (storerClipmap == null)
            {
                return false;
            }

            bool singleClipA = UpdatesOneClipPerFrame();
            bool singleClipB = storerClipmap.UpdatesOneClipPerFrame();
            bool sameClipSet = (storerClipmap.UpdatesPerFrame == VoxelStorageClipmaps.UpdateMethods.SingleClipmap && storerClipmap.ClipMapCurrent == ClipMapCurrent);

            return singleClipA == singleClipB && (!singleClipA || sameClipSet);
        }
        public override int GetHashCode()
        {
            return UpdatesOneClipPerFrame().GetHashCode();
        }


        ObjectParameterKey<Stride.Graphics.Buffer> fragmentsBufferKey;
        ValueParameterKey<Vector3> clipMapResolutionKey;
        ValueParameterKey<float> storageUintsKey;

        ValueParameterKey<float> clipMapCountKey;

        ValueParameterKey<Vector3> clipScaleKey;
        ValueParameterKey<float> clipPosKey;
        ValueParameterKey<Vector3> clipOffsetKey;
        ValueParameterKey<Vector4> perClipMapOffsetScaleKey;
        public void UpdateVoxelizationLayout(string compositionName)
        {
            fragmentsBufferKey = VoxelStorageClipmapShaderKeys.fragmentsBuffer.ComposeWith(compositionName);
            clipMapResolutionKey = VoxelStorageClipmapShaderKeys.clipMapResolution.ComposeWith(compositionName);
            storageUintsKey = VoxelStorageClipmapShaderKeys.storageUints.ComposeWith(compositionName);

            if (UpdatesOneClipPerFrame())
            {
                clipScaleKey = VoxelStorageClipmapShaderKeys.clipScale.ComposeWith(compositionName);
                clipOffsetKey = VoxelStorageClipmapShaderKeys.clipOffset.ComposeWith(compositionName);
                clipPosKey = VoxelStorageClipmapShaderKeys.clipPos.ComposeWith(compositionName);
            }
            else
            {
                clipMapCountKey = VoxelStorageClipmapShaderKeys.clipMapCount.ComposeWith(compositionName);
                perClipMapOffsetScaleKey = VoxelStorageClipmapShaderKeys.perClipMapOffsetScale.ComposeWith(compositionName);
            }
        }
        public void ApplyVoxelizationParameters(ParameterCollection param)
        {
            param.Set(fragmentsBufferKey, FragmentsBuffer);
            param.Set(clipMapResolutionKey, ClipMapResolution);
            param.Set(storageUintsKey, storageUints);

            if (UpdatesOneClipPerFrame())
            {
                param.Set(clipScaleKey, new Vector3(PerMapOffsetScale[ClipMapCurrent].W));
                param.Set(clipOffsetKey, PerMapOffsetScale[ClipMapCurrent].XYZ());
                param.Set(clipPosKey, ClipMapCurrent * ClipMapResolution.X * ClipMapResolution.Y * ClipMapResolution.Z);
            }
            else
            {
                param.Set(clipMapCountKey, ClipMapCount);
                param.Set(perClipMapOffsetScaleKey, PerMapOffsetScale);
            }
        }

        ShaderClassSource storage = new ShaderClassSource("VoxelStorageClipmapShader");

        public ShaderSource GetVoxelizationShader(VoxelizationPass pass, ProcessedVoxelVolume data)
        {
            bool singleClip = UpdatesOneClipPerFrame();
            ShaderSource VoxelizationMethodSource = pass.method.GetVoxelizationShader();
            ShaderMixinSource cachedMixin = new ShaderMixinSource();
            cachedMixin.Mixins.Add(storage);
            cachedMixin.AddComposition("method", VoxelizationMethodSource);
            if (singleClip)
            {
                cachedMixin.AddMacro("singleClip", true);
            }

            string IndirectStoreMacro = "";
            for (int i = 0; i < pass.AttributesIndirect.Count; i++)
            {
                string iStr = i.ToString();
                IndirectStoreMacro += $"AttributesIndirect[{iStr}].IndirectWrite(fragmentsBuffer, writeindex + {pass.AttributesIndirect[i].BufferOffset});\n";
            }

            cachedMixin.AddMacro("IndirectStoreMacro", IndirectStoreMacro);

            foreach (var attr in pass.AttributesTemp)
            {
                cachedMixin.AddCompositionToArray("AttributesTemp", attr.GetVoxelizationShader());
            }
            foreach (var attr in pass.AttributesDirect)
            {
                cachedMixin.AddCompositionToArray("AttributesDirect", attr.GetVoxelizationShader());
            }
            foreach (var attr in pass.AttributesIndirect)
            {
                cachedMixin.AddCompositionToArray("AttributesIndirect", attr.GetVoxelizationShader());
            }

            return cachedMixin;
        }

        public bool RequireGeometryShader()
        {
            return UpdatesPerFrame == VoxelStorageClipmaps.UpdateMethods.AllClipmapsGeometryShader && !UpdatesOneClipPerFrame();
        }
        public int GeometryShaderOutputCount()
        {
            return UpdatesPerFrame == VoxelStorageClipmaps.UpdateMethods.SingleClipmap ? 1 : ClipMapCount;
        }

        public void PostProcess(VoxelStorageContext context, RenderDrawContext drawContext, ProcessedVoxelVolume data)
        {
        }
    }
}
