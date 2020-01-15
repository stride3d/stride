using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core.Mathematics;
using Xenko.Games;
using Xenko.Engine;
using Xenko.Shaders;
using Xenko.Rendering.Voxels.Debug;

namespace Xenko.Rendering.Voxels
{
    public class DataVoxelVolume
    {
        public Vector3 VolumeSize;
        public Vector3 VolumeTranslation;
        public float AproxVoxelSize;
        public bool Voxelize;

        public bool VoxelGridSnapping;
        public bool VisualizeVoxels;
        public IVoxelAttribute VisualizationAttribute;
        public IVoxelVisualization VoxelVisualization;

        public List<IVoxelAttribute> Attributes = new List<IVoxelAttribute>();

        public IVoxelStorage Storage;
        public IVoxelizationMethod VoxelizationMethod;
    }
    public enum VoxelizationStage
    {
        Initial,
        Post
    }
    public struct AttributeStream
    {
        public IVoxelAttribute Attribute;
        public VoxelizationStage Stage;
        public bool Output;
        public AttributeStream(IVoxelAttribute a, VoxelizationStage s, bool o)
        {
            Attribute = a;
            Stage = s;
            Output = o;
        }
    }
    public class ProcessedVoxelVolume
    {
        public bool Voxelize;
        public bool VisualizeVoxels;
        public IVoxelAttribute VisualizationAttribute;
        public IVoxelVisualization VoxelVisualization;

        public IVoxelStorage Storage;
        public IVoxelizationMethod VoxelizationMethod;
        public VoxelStorageContext StorageContext;

        public VoxelizationPassList passList = new VoxelizationPassList();

        public List<List<VoxelizationPass>> groupedPasses = new List<List<VoxelizationPass>>();

        public List<IVoxelAttribute> OutputAttributes = new List<IVoxelAttribute>();
        public List<AttributeStream> Attributes = new List<AttributeStream>();
    }
}
