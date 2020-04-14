using System;
using System.Collections.Generic;
using System.Text;

namespace Xenko.Rendering.Voxels
{
    public struct VoxelViewContext
    {
        public bool IsVoxelView;
        public VoxelViewContext(VoxelizationPassList passes, int viewIndex)
        {
            IsVoxelView = false;
            foreach (var pass in passes.passes)
            {
                if (pass.view.Index == viewIndex)
                {
                    IsVoxelView = true;
                    break;
                }
            }
        }
        public VoxelViewContext(bool voxelView)
        {
            IsVoxelView = voxelView;
        }
    }
}
