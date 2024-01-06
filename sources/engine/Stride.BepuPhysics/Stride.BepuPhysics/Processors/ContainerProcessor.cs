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

        public ContainerProcessor()
        {
            Order = 10000;
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

            component.ContainerData = new(component, _bepuConfiguration, _game);
            component.ContainerData.RebuildContainer();
            var parent = GetComponentsInParents<ContainerComponent>(entity).FirstOrDefault();
            if (parent != null)
            {
                parent.ContainerData?.RebuildContainer();
            }
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            component.ContainerData?.DestroyContainer();
            component.ContainerData = null;

            var parent = GetComponentsInParents<ContainerComponent>(entity).FirstOrDefault();
            if (parent != null)
            {
                parent.ContainerData?.RebuildContainer();
            }
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
