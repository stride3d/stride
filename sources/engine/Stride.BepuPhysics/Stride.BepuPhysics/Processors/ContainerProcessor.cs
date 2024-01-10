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

#warning will not work if we add 2 entities, add a Container to the child Then add a container to the parent. Need rework
            var parent = GetComponentsInParents<ContainerComponent>(entity).FirstOrDefault();
            if (parent != null)
                parent.ChildsContainerComponent.Add(component);

            component.ContainerData = new(component, _bepuConfiguration, _game);
            component.ContainerData.RebuildContainer();
            if (component is ISimulationUpdate simulationUpdate)
                component.ContainerData.BepuSimulation.Register(simulationUpdate);

            OnPostAdd?.Invoke(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            OnPreRemove?.Invoke(component);

            if (component is ISimulationUpdate simulationUpdate)
                component.ContainerData?.BepuSimulation.Unregister(simulationUpdate);
            component.ContainerData?.DestroyContainer();
            component.ContainerData = null;

            var parent = GetComponentsInParents<ContainerComponent>(entity).FirstOrDefault();
            if (parent != null)
                parent.ChildsContainerComponent.Remove(component);
        }

        private static IEnumerable<Entity> GetParents(Entity entity, bool includeMyself = false)
        {
            if (includeMyself)
                yield return entity;

            var parent = entity.GetParent();
            while (parent != null)
            {
                yield return parent;
                parent = parent.GetParent();
            }
        }
        private static IEnumerable<T> GetComponentsInParents<T>(Entity entity, bool includeMyself = false) where T : EntityComponent
        {
            foreach (var parent in GetParents(entity, includeMyself))
            {
                if (parent.Get<T>() is T component)
                    yield return component;
            }
        }

    }
}
