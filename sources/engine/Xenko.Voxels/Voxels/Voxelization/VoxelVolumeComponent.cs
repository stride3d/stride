using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Engine.Design;
using Xenko.Engine.Processors;
using Xenko.Rendering.Voxels.Debug;

namespace Xenko.Rendering.Voxels
{
    /// <summary>
    /// Voxelizes a region.
    /// </summary>
    [DataContract("VoxelVolumeComponent")]
    [DefaultEntityComponentRenderer(typeof(VoxelVolumeProcessor))]
    [Display("Voxel Volume")]
    [ComponentCategory("Lights")]
    public class VoxelVolumeComponent : ActivableEntityComponent
    {
        private bool enabled = true;

        public override bool Enabled
        {
            get { return enabled; }
            set { enabled = value; Changed?.Invoke(this, null); }
        }

        [DataMember(1)]
        public bool Voxelize = true;

        [DataMember(5)]
        [NotNull]
        public IVoxelizationMethod VoxelizationMethod { get; set; } = new VoxelizationMethodDominantAxis();

        [DataMember(10)]
        [NotNull]
        public IVoxelStorage Storage { get; set; } = new VoxelStorageClipmaps();

        [DataMember(20)]
        [Category]
        public List<IVoxelAttribute> Attributes { get; set; } = new List<IVoxelAttribute>();


        [DataMember(30)]
        public float AproximateVoxelSize { get; set; } = 0.15f;
        [DataMember(34)]
        public bool VoxelGridSnapping { get; set; } = true;

        [DataMember(40)]
        [Category]
        public bool VoxelVisualization { get; set; } = false;//Unused, toggle doesn't show if category
        [DataMember(50)]
        public bool VisualizeVoxels { get; set; } = false;
        [DataMember(55)]
        public int VisualizeIndex { get; set; } = 0;
        [DataMember(60)]
        public IVoxelVisualization Visualization { get; set; } = null;

        public event EventHandler Changed;
    }
}
