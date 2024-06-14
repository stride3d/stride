// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class BoxCollider : ColliderBase
{
    private Vector3 _size = new(1, 1, 1);

    /// <remarks>
    /// Changing this value will reset some of the internal physics state of this body
    /// </remarks>
    public Vector3 Size
    {
        get => _size;
        set
        {
            _size = value;
            TryUpdateFeatures();
        }
    }

    internal override void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, NRigidPose localPose)
    {
        builder.Add(new Box(Size.X, Size.Y, Size.Z), localPose, Mass);
    }

    internal override void OnDetach(BufferPool pool) { }
}
