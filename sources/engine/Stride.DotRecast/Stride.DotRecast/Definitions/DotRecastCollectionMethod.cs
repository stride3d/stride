// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.DotRecast.Definitions;

public enum DotRecastCollectionMethod
{
    /// <summary>
    /// Collects all entities in the scene of the entity with the <see cref="NavigationMeshComponent"/>"/>
    /// </summary>
    Scene,

    /// <summary>
    /// Collects all children of the entity with the <see cref="NavigationMeshComponent"/>"/>
    /// </summary>
    Children,

    /// <summary>
    /// Collects all entitys with a valid component in a boundingbox volume
    /// </summary>
    BoundingBox,
}
