using System;
using System.Collections.Generic;
using System.Text;

namespace Xenko.Rendering.Voxels
{
    public struct VoxelViewContext
    {
        public int ViewIndex;
        public bool IsVoxelView;
        public VoxelViewContext(RenderDrawContext context, int viewIndex)
        {
            ViewIndex = viewIndex;
            IsVoxelView = ViewIndex != 0;
        }
    }
}
