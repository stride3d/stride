using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Extensions;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace Stride.BepuPhysics.Processors
{
    public class ContainerProcessor : EntityProcessor<ContainerComponent>
    {
        private IGame? _game = null;
        private BepuConfiguration? _bepuConfiguration = default;

        private readonly UnsortedO1List<IStaticContainer, Matrix4x4> _statics = new();

        internal Dictionary<ContainerComponent, ContainerComponent>.Enumerator ComponentDataEnumerator => base.ComponentDatas.GetEnumerator();

        public event Action<ContainerComponent>? OnPostAdd;
        public event Action<ContainerComponent>? OnPreRemove;

        public ContainerProcessor()
        {
            Order = BepuOrderHelper.ORDER_OF_CONTAINER_P;
        }

        public override void Draw(RenderContext context) // While this is not related to drawing, we're doing this in draw as it runs after the TransformProcessor updates WorldMatrix
        {
            base.Draw(context);

            #warning should be changed to dispatcher's ForBatch from master when it releases
            var span = _statics.UnsafeGetSpan();
            for (int i = 0; i < span.Length; i++)
            {
                var container = span[i].Key;
                ref Matrix4x4 numericMatrix = ref Unsafe.As<Matrix, Matrix4x4>(ref container.Entity.Transform.WorldMatrix); // Casting to numerics, stride's equality comparison is ... not great
                if (span[i].Value == numericMatrix)
                    continue; // This static did not move

                span[i].Value = numericMatrix;
                if (container.ContainerData is null || container.ContainerData.SHandle.Value == -1)
                    continue;

                var description = container.ContainerData.BepuSimulation.Simulation.Statics.GetDescription(container.ContainerData.SHandle);
                container.Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion rotation, out Vector3 translation);
                description.Pose.Position = (translation + container.CenterOfMass).ToNumericVector();
                description.Pose.Orientation = rotation.ToNumericQuaternion();
                container.ContainerData.BepuSimulation.Simulation.Statics.ApplyDescription(container.ContainerData.SHandle, description);
            }
        }

        protected override void OnSystemAdd()
        {
            BepuServicesHelper.LoadBepuServices(Services);
            _game = Services.GetService<IGame>();
            _bepuConfiguration = Services.GetService<BepuConfiguration>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            if (_game == null)
                throw new NullReferenceException(nameof(_game));
            if (_bepuConfiguration == null)
                throw new NullReferenceException(nameof(_bepuConfiguration));

            SetParentForChildren(component, component.Entity.Transform);

            component.ContainerData = new(component, _bepuConfiguration, _game, FindParentContainer(component));
            component.ContainerData.RebuildContainer();
            if (component is ISimulationUpdate simulationUpdate)
                component.ContainerData.BepuSimulation.Register(simulationUpdate);
            if (component is BodyContainerComponent body && body.Interpolation != Interpolation.None)
                component.ContainerData.BepuSimulation.RegisterInterpolated(body);

            if (component is IStaticContainer staticContainer)
                _statics.Add(staticContainer, Unsafe.As<Matrix, Matrix4x4>(ref staticContainer.Entity.Transform.WorldMatrix));

            OnPostAdd?.Invoke(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            OnPreRemove?.Invoke(component);

            if (component is IStaticContainer staticContainer)
                _statics.Remove(staticContainer);

            Debug.Assert(component.ContainerData is not null);

            if (component is ISimulationUpdate simulationUpdate)
                component.ContainerData.BepuSimulation.Unregister(simulationUpdate);
            if (component is BodyContainerComponent body && body.Interpolation != Interpolation.None)
                component.ContainerData.BepuSimulation.UnregisterInterpolated(body);

            if (component.ContainerData.Parent is { } parent) // Make sure that children we leave behind can count on their grand-parent to take care of them
            {
                SetParentForChildren(parent, component.Entity.Transform);
            }

            component.ContainerData.DestroyContainer();
            component.ContainerData = null;
        }

        private static void SetParentForChildren(ContainerComponent parent, TransformComponent root)
        {
            foreach (var child in root.Children)
            {
                if (child.Entity.Get<ContainerComponent>() is { } container)
                {
                    container.ContainerData!.Parent = parent;
                }
                else
                {
                    SetParentForChildren(parent, child);
                }
            }
        }

        private static ContainerComponent? FindParentContainer(ContainerComponent component)
        {
            for (var parent = component.Entity.Transform.Parent; parent != null; parent = parent.Parent)
            {
                if (parent.Entity.Get<ContainerComponent>() is { } comp)
                    return comp;
            }

            return null;
        }
    }
}
