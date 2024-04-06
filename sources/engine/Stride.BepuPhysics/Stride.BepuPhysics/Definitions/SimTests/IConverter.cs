// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.BepuPhysics.Definitions.SimTests;

public interface IConverter<TFrom, TTo>
{
    bool TryConvert(TFrom from, out TTo to);
}

public readonly record struct ManagedConverter(BepuSimulation BepuSimulation) : IConverter<OverlapInfoStack, OverlapInfo>, IConverter<CollidableStack, ContainerComponent>, IConverter<HitInfoStack, HitInfo>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryConvert(OverlapInfoStack from, out OverlapInfo to)
    {
        var container = BepuSimulation.GetContainer(from.CollidableStack.Reference);
        // 'container' can be null when the object the Reference points to was removed from the simulation between when the physics test ran and this execution
        if (container != null! && container.Versioning == from.CollidableStack.Versioning)
        {
            to = new(container, from.PenetrationDirection, from.PenetrationLength);
            return true;
        }

        to = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryConvert(CollidableStack from, out ContainerComponent to)
    {
        to = BepuSimulation.GetContainer(from.Reference);
        // 'to' can be null when the object the Reference points to was removed from the simulation between when the physics test ran and this execution
        return to != null! && to.Versioning == from.Versioning;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryConvert(HitInfoStack from, out HitInfo to)
    {
        var container = BepuSimulation.GetContainer(from.Collidable.Reference);
        // 'container' can be null when the object the Reference points to was removed from the simulation between when the physics test ran and this execution
        if (container != null! && container.Versioning == from.Collidable.Versioning)
        {
            to = new(from.Point, from.Normal, from.Distance, container);
            return true;
        }

        to = default;
        return false;
    }
}
