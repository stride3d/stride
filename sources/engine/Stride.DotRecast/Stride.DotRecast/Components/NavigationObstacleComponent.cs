// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using DotRecast.Detour.Dynamic.Colliders;
using Stride.Core;
using Stride.DotRecast.Definitions;
using Stride.Engine;

namespace Stride.DotRecast.Components;

[DataContract]
[ComponentCategory("DotRecast")]
public class NavigationObstacleComponent : StartupScript
{

    /// <summary>
    /// Determines the navigation layers that this collider will affect.
    /// </summary>
    public NavMeshLayerGroup NavigationLayers { get; set; }

    /// <summary>
    /// The class used to gather the collider information being passed to the dynamic nav mesh.
    /// </summary>
    public required BaseNavigationCollider Collider { get; set; }

    public IDtCollider GetCollider()
    {
        Collider.Initialize(Entity, Services);
        return Collider;
    }
}
