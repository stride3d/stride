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
        private readonly UnsortedO1List<IStaticContainer, Matrix4x4> _statics = new();

        internal BepuShapeCacheSystem ShapeCache { get; private set; } = null!;
        internal Dictionary<ContainerComponent, ContainerComponent>.Enumerator ComponentDataEnumerator => base.ComponentDatas.GetEnumerator();

        public IGame Game { get; private set; } = null!;
        public BepuConfiguration BepuConfiguration { get; private set; } = null!;

        public Action<ContainerComponent>? OnPostAdd;
        public Action<ContainerComponent>? OnPreRemove;

        public ContainerProcessor()
        {
            Order = BepuOrderHelper.ORDER_OF_CONTAINER_P;
        }

        protected override void OnSystemAdd()
        {
            BepuServicesHelper.LoadBepuServices(Services);
            Game = Services.GetService<IGame>();
            BepuConfiguration = Services.GetService<BepuConfiguration>();
            ShapeCache = Game.Services.GetService<BepuShapeCacheSystem>();
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

                if (container.ContainerData?.StaticReference is { } sRef)
                {
                    var description = sRef.GetDescription();
                    container.Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion rotation, out Vector3 translation);
                    description.Pose.Position = (translation + container.CenterOfMass).ToNumericVector();
                    description.Pose.Orientation = rotation.ToNumericQuaternion();
                    sRef.ApplyDescription(description);
                }
            }
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            if (Game == null)
                throw new NullReferenceException(nameof(Game));
            if (BepuConfiguration == null)
                throw new NullReferenceException(nameof(BepuConfiguration));

            component.ContainerData = new(component, this, Game, FindParentContainer(component));
            SetParentForChildren(component.ContainerData, component.Entity.Transform);
            component.ContainerData.RebuildContainer();
            if (component is ISimulationUpdate simulationUpdate)
                component.ContainerData.BepuSimulation.Register(simulationUpdate);
            if (component is BodyContainerComponent body && body.Interpolation != Interpolation.None)
                component.ContainerData.BepuSimulation.RegisterInterpolated(body);

            if (component is IStaticContainer staticContainer)
                _statics.Add(staticContainer, Unsafe.As<Matrix, Matrix4x4>(ref staticContainer.Entity.Transform.WorldMatrix));
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
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

        private static void SetParentForChildren(ContainerData parent, TransformComponent root)
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

        private static ContainerData? FindParentContainer(ContainerComponent component)
        {
            for (var parent = component.Entity.Transform.Parent; parent != null; parent = parent.Parent)
            {
                if (parent.Entity.Get<ContainerComponent>()?.ContainerData is {} containerData)
                    return containerData;
            }

            return null;
        }
    }
}
