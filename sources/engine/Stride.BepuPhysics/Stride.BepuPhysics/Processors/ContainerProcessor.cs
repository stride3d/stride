using System.Diagnostics;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;

namespace Stride.BepuPhysics.Processors
{
    public class ContainerProcessor : EntityProcessor<ContainerComponent>
    {
        private IGame? _game = null;
        private BepuConfiguration? _bepuConfiguration = default;

        internal Dictionary<ContainerComponent, ContainerComponent>.Enumerator ComponentDatas => base.ComponentDatas.GetEnumerator();

        public event Action<ContainerComponent>? OnPostAdd;
        public event Action<ContainerComponent>? OnPreRemove;

        public ContainerProcessor()
        {
            Order = BepuOrderHelper.ORDER_OF_CONTAINER_P;
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

            OnPostAdd?.Invoke(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            OnPreRemove?.Invoke(component);

            Debug.Assert(component.ContainerData is not null);

            if (component is ISimulationUpdate simulationUpdate)
                component.ContainerData.BepuSimulation.Unregister(simulationUpdate);

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
