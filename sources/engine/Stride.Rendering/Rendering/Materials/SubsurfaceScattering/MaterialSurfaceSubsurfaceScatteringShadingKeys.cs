// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Materials
{
    public static partial class MaterialSurfaceSubsurfaceScatteringShadingKeys
    {
        public static readonly ObjectParameterKey<Vector4[]> ScatteringKernel = ParameterKeys.NewObject<Vector4[]>();
    }
}
