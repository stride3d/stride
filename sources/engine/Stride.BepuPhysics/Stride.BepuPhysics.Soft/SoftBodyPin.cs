// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// Holds the particles of a <see cref="SoftBodyComponent"/> found in a region in place, or makes them follow an entity.
/// </summary>
[DataContract]
public sealed class SoftBodyPin
{
    /// <summary>
    /// The particles whose rest position is inside this box are pinned, in the space of the soft body's model.
    /// </summary>
    public BoundingBox Region { get; set; }

    /// <summary>
    /// The entity the pinned particles follow, keeping the offset they had when the soft body was created. Null keeps them where they were created.
    /// </summary>
    public Entity? Anchor { get; set; }
}
