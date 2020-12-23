// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Shadows;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public class VoxelStorageTextureClipmap : IVoxelStorageTexture
    {
        public Vector3 ClipMapResolution;
        public int ClipMapCount;
        public int LayoutSize;
        public float VoxelSize;
        public Int3 VolumeTranslation;

        public bool DownsampleFinerClipMaps;

        public Stride.Graphics.Texture ClipMaps = null;
        public Stride.Graphics.Texture MipMaps = null;
        public Stride.Graphics.Texture[] TempMipMaps = null;

        public Vector4[] PerMapOffsetScale = new Vector4[20];
        public Vector4[] PerMapOffsetScaleCurrent = new Vector4[20];
        public Vector3[] MippingOffset = new Vector3[20];

        ShaderClassSource sampler = new ShaderClassSource("VoxelStorageTextureClipmapShader");

        public void UpdateVoxelizationLayout(string compositionName)
        {

        }
        public void ApplyVoxelizationParameters(ObjectParameterKey<Texture> MainKey, ParameterCollection parameters)
        {
            parameters.Set(MainKey, ClipMaps);
        }

        Stride.Rendering.ComputeEffect.ComputeEffectShader VoxelMipmapSimple;
        //Memory leaks if the ThreadGroupCounts/Numbers/Composition changes (I suppose due to recompiles...?)
        //so instead cache them as seperate shaders.
        Stride.Rendering.ComputeEffect.ComputeEffectShader[][] VoxelMipmapSimpleGroups;

        public void PostProcess(RenderDrawContext drawContext, ShaderSource[] mipmapShaders)
        {
            if (mipmapShaders.Length != LayoutSize)
            {
                return;
            }

            if (VoxelMipmapSimple == null)
            {
                VoxelMipmapSimple = new Stride.Rendering.ComputeEffect.ComputeEffectShader(drawContext.RenderContext) { ShaderSourceName = "Voxel2x2x2MipmapEffect" };
            }

            if (VoxelMipmapSimpleGroups == null || VoxelMipmapSimpleGroups.Length != LayoutSize || VoxelMipmapSimpleGroups[0].Length != TempMipMaps.Length)
            {
                if (VoxelMipmapSimpleGroups != null)
                {
                    for (int axis = 0; axis < LayoutSize; axis++)
                    {
                        if (VoxelMipmapSimpleGroups[axis] != null)
                        {
                            foreach (var shader in VoxelMipmapSimpleGroups[axis])
                            {
                                shader.Dispose();
                            }
                        }
                    }
                }
                VoxelMipmapSimpleGroups = new Stride.Rendering.ComputeEffect.ComputeEffectShader[LayoutSize][];
                for (int axis = 0; axis < LayoutSize; axis++)
                {
                    VoxelMipmapSimpleGroups[axis] = new Stride.Rendering.ComputeEffect.ComputeEffectShader[TempMipMaps.Length];
                    for (int i = 0; i < VoxelMipmapSimpleGroups[axis].Length; i++)
                    {
                        VoxelMipmapSimpleGroups[axis][i] = new Stride.Rendering.ComputeEffect.ComputeEffectShader(drawContext.RenderContext) { ShaderSourceName = "Voxel2x2x2MipmapEffect" };
                    }
                }
            }

            int offsetIndex = 0;
            //Mipmap detailed clipmaps into less detailed ones
            Vector3 totalResolution = ClipMapResolution * new Vector3(1,LayoutSize,1);
            Int3 threadGroupCounts = new Int3(32, 32, 32);
            if (DownsampleFinerClipMaps)
            {
                for (int i = 0; i < ClipMapCount - 1; i++)
                {
                    Vector3 Offset = MippingOffset[offsetIndex];

                    VoxelMipmapSimple.ThreadNumbers = new Int3(8);
                    VoxelMipmapSimple.ThreadGroupCounts = (Int3)((ClipMapResolution / 2f) / (Vector3)VoxelMipmapSimple.ThreadNumbers);

                    for (int axis = 0; axis < LayoutSize; axis++)
                    {
                        VoxelMipmapSimple.Parameters.Set(Voxel2x2x2MipmapKeys.ReadTex, ClipMaps);
                        VoxelMipmapSimple.Parameters.Set(Voxel2x2x2MipmapKeys.WriteTex, TempMipMaps[0]);
                        VoxelMipmapSimple.Parameters.Set(Voxel2x2x2MipmapKeys.ReadOffset, -(Vector3.Mod(Offset, new Vector3(2))) + new Vector3(0, (int)totalResolution.Y * i + (int)ClipMapResolution.Y * axis, 0));
                        VoxelMipmapSimple.Parameters.Set(Voxel2x2x2MipmapKeys.WriteOffset, new Vector3(0, ClipMapResolution.Y / 2 * axis, 0));
                        VoxelMipmapSimple.Parameters.Set(Voxel2x2x2MipmapKeys.mipmapper, mipmapShaders[axis]);
                        ((RendererBase)VoxelMipmapSimple).Draw(drawContext);
                    }

                    Offset -= Vector3.Mod(Offset, new Vector3(2));
                    //Copy each axis, ignoring the top and bottom plane
                    for (int axis = 0; axis < LayoutSize; axis++)
                    {
                        int axisOffset = axis * (int)ClipMapResolution.Y;

                        Int3 CopySize = new Int3((int)ClipMapResolution.X / 2 - 2, (int)ClipMapResolution.Y / 2 - 2, (int)ClipMapResolution.Z / 2 - 2);


                        Int3 DstMinBound = new Int3((int)ClipMapResolution.X / 4 + (int)Offset.X / 2 + 1, (int)totalResolution.Y * (i + 1) + axisOffset + (int)ClipMapResolution.Y / 4 + 1 + (int)Offset.Y / 2, (int)ClipMapResolution.Z / 4 + (int)Offset.Z / 2 + 1);
                        Int3 DstMaxBound = DstMinBound + CopySize;

                        DstMaxBound = Int3.Min(DstMaxBound, new Int3((int)totalResolution.X, (int)totalResolution.Y * (i + 2), (int)totalResolution.Z));
                        DstMinBound = Int3.Min(DstMinBound, new Int3((int)totalResolution.X, (int)totalResolution.Y * (i + 2), (int)totalResolution.Z));
                        DstMaxBound = Int3.Max(DstMaxBound, new Int3(0, (int)totalResolution.Y * (i + 1), 0));
                        DstMinBound = Int3.Max(DstMinBound, new Int3(0, (int)totalResolution.Y * (i + 1), 0));

                        Int3 SizeBound = DstMaxBound - DstMinBound;

                        Int3 SrcMinBound = new Int3(1, axisOffset / 2 + 1, 1);
                        Int3 SrcMaxBound = SrcMinBound + SizeBound;

                        if (SizeBound.X > 0 && SizeBound.Y > 0 && SizeBound.Z > 0)
                        {
                            drawContext.CommandList.CopyRegion(TempMipMaps[0], 0,
                                new ResourceRegion(
                                    SrcMinBound.X, SrcMinBound.Y, SrcMinBound.Z,
                                    SrcMaxBound.X, SrcMaxBound.Y, SrcMaxBound.Z
                                ),
                                ClipMaps, 0,
                                DstMinBound.X, DstMinBound.Y, DstMinBound.Z);
                        }
                    }
                    offsetIndex++;
                }
            }
            Vector3 resolution = ClipMapResolution;
            offsetIndex = ClipMapCount-1;
            //Mipmaps for the largest clipmap
            for (int i = 0; i < TempMipMaps.Length - 1; i++)
            {
                Vector3 Offset = MippingOffset[offsetIndex];
                resolution /= 2;

                Vector3 threadNums = Vector3.Min(resolution, new Vector3(8));

                for (int axis = 0; axis < LayoutSize; axis++)
                {
                    var mipmapShader = VoxelMipmapSimpleGroups[axis][i];
                    mipmapShader.ThreadNumbers = (Int3)(threadNums);
                    mipmapShader.ThreadGroupCounts = (Int3)(resolution / threadNums);
                    if (i == 0)
                    {
                        mipmapShader.Parameters.Set(Voxel2x2x2MipmapKeys.ReadTex, ClipMaps);
                        mipmapShader.Parameters.Set(Voxel2x2x2MipmapKeys.ReadOffset, -Offset + new Vector3(0, (int)ClipMapResolution.Y * LayoutSize * (ClipMapCount - 1) + (int)ClipMapResolution.Y * axis, 0));
                        mipmapShader.Parameters.Set(Voxel2x2x2MipmapKeys.WriteOffset, new Vector3(0, resolution.Y * axis, 0));
                    }
                    else
                    {
                        mipmapShader.Parameters.Set(Voxel2x2x2MipmapKeys.ReadTex, TempMipMaps[i - 1]);
                        mipmapShader.Parameters.Set(Voxel2x2x2MipmapKeys.ReadOffset, -Offset + new Vector3(0, resolution.Y * axis * 2, 0));
                        mipmapShader.Parameters.Set(Voxel2x2x2MipmapKeys.WriteOffset, new Vector3(0, resolution.Y * axis, 0));
                    }
                    mipmapShader.Parameters.Set(Voxel2x2x2MipmapKeys.WriteTex, TempMipMaps[i]);
                    mipmapShader.Parameters.Set(Voxel2x2x2MipmapKeys.mipmapper, mipmapShaders[axis]);
                    ((RendererBase)mipmapShader).Draw(drawContext);
                }
                //Don't seem to be able to read and write to the same texture, even if the views
                //point to different mipmaps.
                drawContext.CommandList.CopyRegion(TempMipMaps[i], 0, null, MipMaps, i);
                offsetIndex++;
            }
            Array.Copy(PerMapOffsetScale, PerMapOffsetScaleCurrent, PerMapOffsetScale.Length);
        }




        private ObjectParameterKey<Texture> ClipMapskey;
        private ObjectParameterKey<Texture> MipMapskey;
        private ValueParameterKey<Vector4> perMapOffsetScaleKey;
        public void UpdateSamplingLayout(string compositionName)
        {
            ClipMapskey = VoxelStorageTextureClipmapShaderKeys.clipMaps.ComposeWith(compositionName);
            MipMapskey = VoxelStorageTextureClipmapShaderKeys.mipMaps.ComposeWith(compositionName);
            perMapOffsetScaleKey = VoxelStorageTextureClipmapShaderKeys.perMapOffsetScale.ComposeWith(compositionName);
        }
        public ShaderClassSource GetSamplingShader()
        {
            sampler = new ShaderClassSource("VoxelStorageTextureClipmapShader", VoxelSize, ClipMapCount, LayoutSize, ClipMapResolution.Y/2.0f);
            return sampler;
        }
        public void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            parameters.Set(ClipMapskey, ClipMaps);
            parameters.Set(MipMapskey, MipMaps);
            parameters.Set(perMapOffsetScaleKey, viewContext.IsVoxelView? PerMapOffsetScaleCurrent : PerMapOffsetScale);
        }
    }
}
