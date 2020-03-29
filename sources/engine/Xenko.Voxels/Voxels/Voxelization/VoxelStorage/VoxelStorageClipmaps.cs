using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Clipmaps")]
    public class VoxelStorageClipmaps : IVoxelStorage
    {
        public enum Resolutions{
            x32 = 32,
            x64 = 64,
            x128 = 128,
            x256 = 256
        };
        public enum UpdateMethods
        {
            [Display("Single Clipmap")] SingleClipmap,
            [Display("All Clipmaps (Geometry Shader)")] AllClipmapsGeometryShader,
            [Display("All Clipmaps (Multiple Renders)")] AllClipmapsMultipleRenders,
        };

        [DataMember(0)]
        public Resolutions ClipResolution { get; set; } = Resolutions.x128;
        [DataMember(10)]
        public UpdateMethods UpdatesPerFrame { get; set; } = UpdateMethods.SingleClipmap;
        [DataMember(20)]
        public bool DownsampleFinerClipMaps { get; set; } = true;

        int storageUints;
        Xenko.Graphics.Buffer FragmentsBuffer = null;

        int ClipMapCount;
        int MipMapCount;
        int ClipMapCurrent = -1;
        Vector3 ClipMapResolution;

        Vector3[] PerMapSnappingOffset = new Vector3[20];
        Vector4[] PerMapOffsetScale = new Vector4[20];
        Vector3[] MippingOffset = new Vector3[20];
        Int3[] MippingOffsetTranslation = new Int3[20];

        bool ShouldUpdateClipIndex(int i)
        {
            return i == ClipMapCurrent || UpdatesPerFrame != UpdateMethods.SingleClipmap || (i >= ClipMapCount && ClipMapCurrent == ClipMapCount - 1);
        }
        
        public void UpdateFromContext(VoxelStorageContext context)
        {
            var virtualResolution = context.Resolution();
            var largestDimension = (double)Math.Max(virtualResolution.X, Math.Max(virtualResolution.Y, virtualResolution.Z));

            ClipMapCount = (int)Math.Log(largestDimension / Math.Min(largestDimension, (double)ClipResolution), 2) + 1;

            ClipMapCurrent++;
            if (ClipMapCurrent >= ClipMapCount)
            {
                ClipMapCurrent = 0;
            }

            float FinestClipMapScale = (float)Math.Pow(2, ClipMapCount - 1);
            ClipMapResolution = new Vector3(virtualResolution.X, virtualResolution.Y, virtualResolution.Z) / FinestClipMapScale;
            MipMapCount = (int)Math.Floor(Math.Log(Math.Min(ClipMapResolution.X, Math.Min(ClipMapResolution.Y, ClipMapResolution.Z)), 2));

            int voxelScale = 1;

            for (int i = 0; i < (ClipMapCount + MipMapCount); i++)
            {
                Vector3 SnappedVolumeTranslation = context.VoxelSpaceTranslation;
                SnappedVolumeTranslation.X = (float)Math.Floor(SnappedVolumeTranslation.X / voxelScale) * voxelScale;
                SnappedVolumeTranslation.Y = (float)Math.Floor(SnappedVolumeTranslation.Y / voxelScale) * voxelScale;
                SnappedVolumeTranslation.Z = (float)Math.Floor(SnappedVolumeTranslation.Z / voxelScale) * voxelScale;

                if (ShouldUpdateClipIndex(i))
                {
                    PerMapSnappingOffset[i] = -SnappedVolumeTranslation * context.RealVoxelSize();
                    MippingOffsetTranslation[i] = new Int3((int)SnappedVolumeTranslation.X, (int)SnappedVolumeTranslation.Y, (int)SnappedVolumeTranslation.Z);
                }

                voxelScale *= 2;
            }

            float extentScale = (float)Math.Pow(2f, ClipMapCount - 1);
            voxelScale = 1;

            for (int i = 0; i < (ClipMapCount + MipMapCount); i++)
            {
                if (ShouldUpdateClipIndex(i))
                {
                    Vector3 offset = (PerMapSnappingOffset[i]) * extentScale / context.Extents + 0.5f;
                    PerMapOffsetScale[i] = new Vector4(offset, (1.0f / context.Extents.X) * extentScale);
                }

                if (i + 1 == ClipMapCurrent || ShouldUpdateClipIndex(i))
                {
                    MippingOffset[i] = (Vector3)((MippingOffsetTranslation[i] - MippingOffsetTranslation[i + 1]) / voxelScale);
                }

                if (i < ClipMapCount - 1)
                {
                    extentScale /= 2;
                }
                voxelScale *= 2;
            }

        }


        int tempStorageCounter;
        public int RequestTempStorage(int count)
        {
            int bufferOffset = tempStorageCounter / 32;
            tempStorageCounter += ((count+31)/32)*32;
            return bufferOffset;
        }

        public void UpdateTempStorage(VoxelStorageContext context)
        {
            storageUints = (tempStorageCounter + 31)/32;
            tempStorageCounter = 0;

            var resolution = ClipMapResolution;
            int fragments = (int)(resolution.X * resolution.Y * resolution.Z) * ClipMapCount;

            if (VoxelUtils.DisposeBufferBySpecs(FragmentsBuffer, storageUints * fragments) && storageUints * fragments > 0)
            {
                FragmentsBuffer = Xenko.Graphics.Buffer.Typed.New(context.device, storageUints * fragments, PixelFormat.R32_UInt, true);
            }
        }



        public void UpdateTexture(VoxelStorageContext context, ref IVoxelStorageTexture texture, Xenko.Graphics.PixelFormat pixelFormat, int LayoutSize)
        {
            VoxelStorageTextureClipmap clipmap = texture as VoxelStorageTextureClipmap;
            if (clipmap == null)
            {
                clipmap = new VoxelStorageTextureClipmap();
            }

            Vector3 ClipMapTextureResolution = new Vector3(ClipMapResolution.X, ClipMapResolution.Y * ClipMapCount * LayoutSize, ClipMapResolution.Z);
            Vector3 MipMapResolution = new Vector3(ClipMapResolution.X / 2, ClipMapResolution.Y / 2 * LayoutSize, ClipMapResolution.Z / 2);
            if (VoxelUtils.DisposeTextureBySpecs(clipmap.ClipMaps, ClipMapTextureResolution, pixelFormat))
            {
                clipmap.ClipMaps = Xenko.Graphics.Texture.New3D(context.device, (int)ClipMapTextureResolution.X, (int)ClipMapTextureResolution.Y, (int)ClipMapTextureResolution.Z, new MipMapCount(false), pixelFormat, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            }
            if (VoxelUtils.DisposeTextureBySpecs(clipmap.MipMaps, MipMapResolution, pixelFormat))
            {
                if (clipmap.TempMipMaps != null)
                {
                    for (int i = 0; i < clipmap.TempMipMaps.Length; i++)
                    {
                        clipmap.TempMipMaps[i].Dispose();
                    }
                }

                Vector3 MipMapResolutionMax = MipMapResolution;

                clipmap.MipMaps = Xenko.Graphics.Texture.New3D(context.device, (int)MipMapResolution.X, (int)MipMapResolution.Y, (int)MipMapResolution.Z, new MipMapCount(true), pixelFormat, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);

                clipmap.TempMipMaps = new Xenko.Graphics.Texture[MipMapCount];

                for (int i = 0; i < clipmap.TempMipMaps.Length; i++)
                {
                    clipmap.TempMipMaps[i] = Xenko.Graphics.Texture.New3D(context.device, (int)MipMapResolutionMax.X, (int)MipMapResolutionMax.Y, (int)MipMapResolutionMax.Z, false, pixelFormat, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);

                    MipMapResolutionMax /= 2;
                }
            }
            clipmap.DownsampleFinerClipMaps = DownsampleFinerClipMaps;
            clipmap.ClipMapResolution = ClipMapResolution;
            clipmap.ClipMapCount = ClipMapCount;
            clipmap.LayoutSize = LayoutSize;
            clipmap.VoxelSize = context.RealVoxelSize();
            clipmap.VolumeTranslation = new Int3((int)context.VoxelSpaceTranslation.X, (int)context.VoxelSpaceTranslation.Y, (int)context.VoxelSpaceTranslation.Z);

            Array.Copy(MippingOffset, clipmap.MippingOffset, MippingOffset.Length);
            Array.Copy(PerMapOffsetScale, clipmap.PerMapOffsetScale, PerMapOffsetScale.Length);



            texture = clipmap;
        }

        public void CollectVoxelizationPasses(ProcessedVoxelVolume data, VoxelStorageContext storageContext)
        {
            Matrix BaseVoxelMatrix = storageContext.Matrix;
            BaseVoxelMatrix.Invert();
            BaseVoxelMatrix = BaseVoxelMatrix * Matrix.Scaling(2f, 2f, 2f);
            if (UpdatesPerFrame != UpdateMethods.AllClipmapsMultipleRenders)
            {
                /*
                 * Having trouble with shadow culling when this is enabled - currently performed in vertex shader instead
                if (UpdateMethod == UpdateMethods.OneClipPerFrame)
                {
                    BaseVoxelMatrix = Matrix.Scaling(PerMapOffsetScale[ClipMapCurrent].W) * Matrix.Translation(PerMapOffsetScale[ClipMapCurrent].XYZ());
                    BaseVoxelMatrix = BaseVoxelMatrix * Matrix.Translation(-0.5f,-0.5f,-0.5f);
                    BaseVoxelMatrix = BaseVoxelMatrix * Matrix.Scaling(2f, 2f, 2f);
                }
                */
                VoxelStorerClipmap Storer = new VoxelStorerClipmap
                {
                    storageUints = storageUints,
                    FragmentsBuffer = FragmentsBuffer,
                    ClipMapCount = ClipMapCount,
                    ClipMapCurrent = ClipMapCurrent,
                    ClipMapResolution = ClipMapResolution,
                    PerMapOffsetScale = PerMapOffsetScale,
                    UpdatesPerFrame = UpdatesPerFrame
                };
                foreach (var attr in data.Attributes)
                {
                    attr.Attribute.CollectVoxelizationPasses(data.passList, Storer, BaseVoxelMatrix, ClipMapResolution, attr.Stage, attr.Output);
                }
            }
            if (UpdatesPerFrame == UpdateMethods.AllClipmapsMultipleRenders)
            {
                for (int i = 0; i < ClipMapCount; i++)
                {
                    VoxelStorerClipmap Storer = new VoxelStorerClipmap
                    {
                        storageUints = storageUints,
                        FragmentsBuffer = FragmentsBuffer,
                        ClipMapCount = ClipMapCount,
                        ClipMapCurrent = i,
                        ClipMapResolution = ClipMapResolution,
                        PerMapOffsetScale = PerMapOffsetScale,
                        UpdatesPerFrame = UpdateMethods.SingleClipmap
                    };
                    foreach (var attr in data.Attributes)
                    {
                        attr.Attribute.CollectVoxelizationPasses(data.passList, Storer, BaseVoxelMatrix, ClipMapResolution, attr.Stage, attr.Output);
                    }
                }
            }
        }

        Xenko.Rendering.ComputeEffect.ComputeEffectShader BufferToTexture;
        Xenko.Rendering.ComputeEffect.ComputeEffectShader BufferToTextureColumns;
        Xenko.Rendering.ComputeEffect.ComputeEffectShader ClearBuffer;

        public void PostProcess(VoxelStorageContext storageContext, RenderDrawContext drawContext, ProcessedVoxelVolume data)
        {
            if (Math.Max(Math.Max(ClipMapResolution.X, ClipMapResolution.Y), ClipMapResolution.Z) < 32)
                return;
            if (FragmentsBuffer == null)
                return;
            var context = drawContext.RenderContext;
            if (ClearBuffer == null)
            {
                ClearBuffer = new Xenko.Rendering.ComputeEffect.ComputeEffectShader(context) { ShaderSourceName = "ClearBuffer" };
                BufferToTexture = new Xenko.Rendering.ComputeEffect.ComputeEffectShader(context) { ShaderSourceName = "BufferToTextureEffect" };
                BufferToTextureColumns = new Xenko.Rendering.ComputeEffect.ComputeEffectShader(context) { ShaderSourceName = "BufferToTextureColumnsEffect" };
            }

            bool VoxelsAreIndependent = true;

            List<VoxelAttribute> IndirectVoxels = new List<VoxelAttribute>();
            List<VoxelAttribute> TempVoxels = new List<VoxelAttribute>();
            ShaderSourceCollection Indirect = new ShaderSourceCollection();
            ShaderSourceCollection Temp = new ShaderSourceCollection();

            //Assign sample indices and check whether voxels can be calculated independently
            int sampleIndex = 0;
            foreach (var attr in data.Attributes)
            {
                attr.Attribute.LocalSamplerID = sampleIndex;
                VoxelsAreIndependent &= !attr.Attribute.RequiresColumns();
                sampleIndex++;
            }

            //Populate ShaderSourceCollections and temp lists
            foreach (var attr in data.Attributes)
            {
                if (attr.Stage != VoxelizationStage.Post) continue;
                if (attr.Output)
                {
                    Indirect.Add(attr.Attribute.GetVoxelizationShader());
                    IndirectVoxels.Add(attr.Attribute);
                }
                else
                {
                    Temp.Add(attr.Attribute.GetVoxelizationShader());
                    TempVoxels.Add(attr.Attribute);
                }
            }

            var BufferWriter = VoxelsAreIndependent ? BufferToTexture : BufferToTextureColumns;

            for (int i = 0; i < IndirectVoxels.Count; i++)
            {
                var attr = IndirectVoxels[i];
                attr.UpdateVoxelizationLayout($"AttributesIndirect[{i}]");
            }
            for (int i = 0; i < TempVoxels.Count; i++)
            {
                var attr = TempVoxels[i];
                attr.UpdateVoxelizationLayout($"AttributesTemp[{i}]");
            }
            foreach (var attr in data.Attributes)
            {
                attr.Attribute.ApplyVoxelizationParameters(BufferWriter.Parameters);
            }


            int processYSize = VoxelsAreIndependent ? (int)ClipMapResolution.Y : 1;
            processYSize *= (UpdatesPerFrame == UpdateMethods.SingleClipmap) ? 1 : ClipMapCount;

            BufferWriter.ThreadGroupCounts = VoxelsAreIndependent ? new Int3(32, 32, 32) : new Int3(32, 1, 32);
            BufferWriter.ThreadNumbers = new Int3((int)ClipMapResolution.X / BufferWriter.ThreadGroupCounts.X, processYSize / BufferWriter.ThreadGroupCounts.Y, (int)ClipMapResolution.Z / BufferWriter.ThreadGroupCounts.Z);

            BufferWriter.Parameters.Set(BufferToTextureKeys.VoxelFragments, FragmentsBuffer);
            BufferWriter.Parameters.Set(BufferToTextureKeys.clipMapResolution, ClipMapResolution);
            BufferWriter.Parameters.Set(BufferToTextureKeys.storageUints, storageUints);

            BufferWriter.Parameters.Set(BufferToTextureKeys.clipOffset, (uint)(UpdatesPerFrame == UpdateMethods.SingleClipmap ? ClipMapCurrent : 0));


            //Modifiers are stored within attributes, yet need to be able to query their results. 
            //Ideally a stage stream could resolve this, however due to the lack of pointers, there would be a cyclic dependency of AttributesList->Attribute->Modifier->AttributesList->...
            //So instead the results will be stored within a second array that only contains float4s. Unfortunately the only way to iterate through the AttributesList is by foreach, which
            //makes it difficult to access the results array (AttributeLocalSamples) by index. So instead it's just all done through this macro...
            string IndirectReadAndStoreMacro = "";
            string IndirectStoreMacro = "";
            for (int i = 0; i < Temp.Count; i ++)
            {
                string iStr = i.ToString();
                string sampleIndexStr = TempVoxels[i].LocalSamplerID.ToString();
                IndirectReadAndStoreMacro += $"AttributesTemp[{iStr}].InitializeFromBuffer(VoxelFragments, VoxelFragmentsIndex + {TempVoxels[i].BufferOffset}, uint2({TempVoxels[i].BufferOffset} + initialVoxelFragmentsIndex, yStride));\n" +
                                             $"streams.LocalSample[{sampleIndexStr}] = AttributesTemp[{iStr}].SampleLocal();\n\n";
                IndirectStoreMacro += $"streams.LocalSample[{sampleIndexStr}] = AttributesTemp[{iStr}].SampleLocal();\n";
            }
            for (int i = 0; i < Indirect.Count; i++)
            {
                string iStr = i.ToString();
                string sampleIndexStr = IndirectVoxels[i].LocalSamplerID.ToString();
                IndirectReadAndStoreMacro += $"AttributesIndirect[{iStr}].InitializeFromBuffer(VoxelFragments, VoxelFragmentsIndex + {IndirectVoxels[i].BufferOffset}, uint2({IndirectVoxels[i].BufferOffset} + initialVoxelFragmentsIndex, yStride));\n" +
                                             $"streams.LocalSample[{sampleIndexStr}] = AttributesIndirect[{iStr}].SampleLocal();\n\n";
                IndirectStoreMacro += $"streams.LocalSample[{sampleIndexStr}] = AttributesIndirect[{iStr}].SampleLocal();\n";
            }



            BufferWriter.Parameters.Set(BufferToTextureKeys.AttributesIndirect, Indirect);
            BufferWriter.Parameters.Set(BufferToTextureKeys.AttributesTemp, Temp);
            BufferWriter.Parameters.Set(BufferToTextureKeys.IndirectReadAndStoreMacro, IndirectReadAndStoreMacro);
            BufferWriter.Parameters.Set(BufferToTextureKeys.IndirectStoreMacro, IndirectStoreMacro);

            ((RendererBase)BufferWriter).Draw(drawContext);



            ClearBuffer.Parameters.Set(ClearBufferKeys.buffer, FragmentsBuffer);

            if (UpdatesPerFrame != UpdateMethods.SingleClipmap)
            {
                //Clear all
                ClearBuffer.ThreadNumbers = new Int3(1024, 1, 1);
                ClearBuffer.ThreadGroupCounts = new Int3(FragmentsBuffer.ElementCount / 1024, 1, 1);
                ClearBuffer.Parameters.Set(ClearBufferKeys.offset, 0);
            }
            else
            {
                //Clear next clipmap buffer
                ClearBuffer.ThreadNumbers = new Int3(1024, 1, 1);
                ClearBuffer.ThreadGroupCounts = new Int3((int)(ClipMapResolution.X * ClipMapResolution.Y * ClipMapResolution.Z * storageUints) / 1024, 1, 1);
                ClearBuffer.Parameters.Set(ClearBufferKeys.offset, (int)(((ClipMapCurrent+1) % ClipMapCount) * ClipMapResolution.X * ClipMapResolution.Y * ClipMapResolution.Z * storageUints));
            }
            ((RendererBase)ClearBuffer).Draw(drawContext);
        }
    }
}
