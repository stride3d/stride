﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Images;

namespace Stride.Rendering.Voxels.Debug
{
    public interface IVoxelVisualization
    {
        ImageEffectShader GetShader(RenderDrawContext context, VoxelAttribute attr);
    }
}
