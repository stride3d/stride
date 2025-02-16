// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using DotRecast.Detour;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Navigation.Definitions;
public class DynamicTile : DtMeshTile
{
    public DynamicTile(int index) : base(index)
    {
    }

    public BoundingBox BoundingBox { get; set; }

}
