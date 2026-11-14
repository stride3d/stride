// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Rendering;

namespace Stride.BepuPhysics.Soft.Topology;

/// <summary>
/// Reads the world matrices of the nodes of an entity's model, such as the bones of an animated skeleton.
/// </summary>
internal static class ModelNodes
{
    /// <summary> The skeleton of the entity's model with its world matrices recomputed from the current transform and node poses, null without one </summary>
    /// <remarks> The model only does it before drawing, the physics step would otherwise read the poses of the previous frame </remarks>
    public static SkeletonUpdater? Refresh(Entity entity)
    {
        if (entity.Get<ModelComponent>()?.Skeleton is not { } skeleton || skeleton.NodeTransformations.Length == 0)
            return null;

        entity.Transform.UpdateWorldMatrix();
        skeleton.NodeTransformations[0].LocalMatrix = entity.Transform.WorldMatrix;
        skeleton.UpdateMatrices();
        return skeleton;
    }

    /// <summary> The index of the node called <paramref name="name"/>, or -1 </summary>
    public static int Find(SkeletonUpdater skeleton, string name)
    {
        var nodes = skeleton.Nodes;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].Name == name)
                return i;
        }
        return -1;
    }
}
