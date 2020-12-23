// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Rendering.Voxels.Debug;

namespace Stride.Rendering.Voxels
{
    /// <summary>
    /// Voxelizes a region.
    /// </summary>
    [DataContract("VoxelVolumeComponent")]
    [DefaultEntityComponentRenderer(typeof(VoxelVolumeProcessor))]
    [Display("Voxel Volume", Expand = ExpandRule.Once)]
    [ComponentCategory("Lights")]
    [CategoryOrder(10, "Attributes")]
    [CategoryOrder(60, "Visualization/Debug")]
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

        [DataMember(10)]
        [NotNull]
        public IVoxelizationMethod VoxelizationMethod { get; set; } = new VoxelizationMethodDominantAxis();

        [DataMember(20)]
        [NotNull]
        public IVoxelStorage Storage { get; set; } = new VoxelStorageClipmaps();

        [DataMember(30)]
        [Category]
        public List<VoxelAttribute> Attributes { get; set; } = new List<VoxelAttribute>();


        [DataMember(35)]
        public float VoxelVolumeSize { get; set; } = 20f;
        [DataMember(40)]
        public float AproximateVoxelSize { get; set; } = 0.15f;
        [DataMember(50)]
        public bool VoxelGridSnapping { get; set; } = true;

        [DataMember(60)]
        [Display(category: "Visualization/Debug")]
        public bool VisualizeVoxels { get; set; } = false;
        [DataMember(70)]
        [Display(category: "Visualization/Debug")]
        public int VisualizeIndex { get; set; } = 0;
        [DataMember(80)]
        [Display(category: "Visualization/Debug")]
        public IVoxelVisualization Visualization { get; set; } = null;

        public event EventHandler Changed;
    }
}
