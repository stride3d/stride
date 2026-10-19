// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;

namespace Stride.Rendering.Materials.ComputeColors;

[DataContract("ComputeClampedLerpColor")]
[Display("Lerp Operator")]
public class ComputeClampedLerpColor : ComputeClampedLerpBase<IComputeColor>, IComputeColor
{
    public bool HasChanged
    {
        get
        {
            // Null children force skip changes
            if (White == null || Black == null || Blend == null)
                return false;

            return White.HasChanged || Black.HasChanged || Blend.HasChanged;
        }
    }
}
