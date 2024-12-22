// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.DotRecast.Definitions;

namespace Stride.BepuPhysics.Navigation.Definitions;

[DataContract]
public class BepuNavMeshInfo
{
    public BuildSettings BuildSettings { get; set; } = new();

    /// <summary>
    /// Collision masks that will be included in the navigation mesh build.
    /// </summary>
    public CollisionMask CollisionMask { get; set; } = CollisionMask.Everything;
}
