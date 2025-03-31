// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine.FlexibleProcessing;
using Stride.Games;

namespace Stride.DotRecast.Definitions;

public interface INavigationObstacle
{

    /// <summary>
    /// Determines the navigation layers that this collider will affect.
    /// </summary>
    public NavMeshLayerGroup NavigationLayers { get; set; }

    /// <summary>
    /// Hash value of the collider. Used to determine if the collider has changed.
    /// </summary>
    /// <returns></returns>
    public bool ColliderValueHash();

    public DotRecastShapeData GetNavigationMeshInputData();
}
