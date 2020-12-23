// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Rendering.Voxels
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
