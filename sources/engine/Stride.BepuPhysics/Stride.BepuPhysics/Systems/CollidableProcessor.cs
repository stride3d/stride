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
    private PoseToUpdate[] _workingBuffer = Array.Empty<PoseToUpdate>();
    private int _workingBufferHead = 0;

    internal readonly UnsortedO1List<StaticComponent, Matrix4x4> Statics = new();

    internal ShapeCacheSystem ShapeCache { get; private set; } = null!;
    internal Dictionary<CollidableComponent, CollidableComponent>.Enumerator ComponentDataEnumerator => base.ComponentDatas.GetEnumerator();

    public BepuConfiguration BepuConfiguration { get; private set; } = null!;

    public Action<CollidableComponent>? OnPostAdd;
    public Action<CollidableComponent>? OnPreRemove;

    private struct PoseToUpdate
    {
        public StaticComponent Component;
        public Vector3 Position;
        public Quaternion Rotation;
    }

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
        _workingBufferHead = 0;
        if (Statics.Count > _workingBuffer.Length)
            _workingBuffer = new PoseToUpdate[System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Statics.Count)];

        Dispatcher.ForBatched(Statics.Count, this, &Collect);

        foreach (var poseToUpdate in _workingBuffer.AsSpan()[.._workingBufferHead])
            poseToUpdate.Component.TeleportNoTransformUpdate(poseToUpdate.Position, poseToUpdate.Rotation);

        static void Collect(CollidableProcessor @this, int from, int toExclusive)
        {
            var span = @this.Statics.UnsafeGetSpan();
            for (int i = from; i < toExclusive; i++)
            {
                ref var iData = ref span[i];
                var collidable = iData.Key;

                ref Matrix4x4 numericMatrix = ref Unsafe.As<Matrix, Matrix4x4>(ref collidable.Entity.Transform.WorldMatrix); // Casting to numerics, stride's equality comparison is ... not great
                if (iData.Value == numericMatrix)
                    continue; // This static did not move

                iData.Value = numericMatrix;

                PoseToUpdate poseToUpdate;
                poseToUpdate.Component = collidable;
                collidable.Entity.Transform.WorldMatrix.Decompose(out _, out poseToUpdate.Rotation, out poseToUpdate.Position);
                @this._workingBuffer[Interlocked.Increment(ref @this._workingBufferHead) - 1] = poseToUpdate;
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

        component.Detach();

        component.Processor = null;
    }
}
