// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;

namespace Stride.DotRecast.Definitions;

/// <summary>
/// Used to determine static geometry/shapes that can be used with a navigation mesh.
/// </summary>
[DataContract(Inherited = true)]
public abstract class DotRecastGeometryProvider
{
    [DataMemberIgnore]
    public IServiceRegistry Services;

    internal void Initialize(IServiceRegistry registry)
    {
        Services = registry;
    }

    /// <summary>
    /// Tries to get the shape information for the geometry.
    /// </summary>
    /// <returns></returns>
    public abstract bool TryGetTransformedShapeInfo(Entity entity, out DotRecastShapeData shapeData);
}
