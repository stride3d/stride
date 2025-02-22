// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.BepuPhysics.Components;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Stride.BepuPhysics.Definitions;
using Stride.Core.Threading;

namespace Stride.BepuPhysics.Systems;

public class CollidableProcessor : EntityProcessor<CollidableComponent>
{
    internal readonly UnsortedO1List<StaticComponent, Matrix4x4> Statics = new();

    internal ShapeCacheSystem ShapeCache { get; private set; } = null!;
    internal Dictionary<CollidableComponent, CollidableComponent>.Enumerator ComponentDataEnumerator => base.ComponentDatas.GetEnumerator();

    public BepuConfiguration BepuConfiguration { get; private set; } = null!;

    public Action<CollidableComponent>? OnPostAdd;
    public Action<CollidableComponent>? OnPreRemove;

    public CollidableProcessor()
    {
        Order = SystemsOrderHelper.ORDER_OF_COLLIDABLE_P;
    }

    protected override void OnSystemAdd()
    {
        BepuConfiguration = Services.GetOrCreate<BepuConfiguration>();
        ShapeCache = Services.GetOrCreate<ShapeCacheSystem>();
    }

    public override unsafe void Draw(RenderContext context) // While this is not related to drawing, we're doing this in draw as it runs after the TransformProcessor updates WorldMatrix
    {
        Dispatcher.ForBatched(Statics.Count, Statics, &Process);

        static void Process(UnsortedO1List<StaticComponent, Matrix4x4> statics, int from, int toExclusive)
        {
            Span<UnsortedO1List<StaticComponent, Matrix4x4>.SequentialData> span = statics.UnsafeGetSpan();
            for (int i = from; i < toExclusive; i++)
            {
                var collidable = span[i].Key;
                ref Matrix4x4 numericMatrix = ref Unsafe.As<Matrix, Matrix4x4>(ref collidable.Entity.Transform.WorldMatrix); // Casting to numerics, stride's equality comparison is ... not great
                if (span[i].Value == numericMatrix)
                    continue; // This static did not move

                span[i].Value = numericMatrix;

                if (collidable.StaticReference is { } sRef)
                {
                    var description = sRef.GetDescription();
                    collidable.Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion rotation, out Vector3 translation);
                    description.Pose.Position = (translation + collidable.CenterOfMass).ToNumeric();
                    description.Pose.Orientation = rotation.ToNumeric();
                    sRef.ApplyDescription(description);
                }
            }
        }
    }

    protected override void OnEntityComponentAdding(Entity entity, CollidableComponent component, CollidableComponent data)
    {
        Debug.Assert(BepuConfiguration is not null);

        component.Processor = this;

        var targetSimulation = component.SimulationSelector.Pick(BepuConfiguration, component.Entity);
        component.ReAttach(targetSimulation);

        if (component is ISimulationUpdate simulationUpdate)
            targetSimulation.Register(simulationUpdate);
    }

    protected override void OnEntityComponentRemoved(Entity entity, CollidableComponent component, CollidableComponent data)
    {
        if (component is ISimulationUpdate simulationUpdate)
            component.Simulation?.Unregister(simulationUpdate);

        component.Detach(false);

        component.Processor = null;
    }
}
