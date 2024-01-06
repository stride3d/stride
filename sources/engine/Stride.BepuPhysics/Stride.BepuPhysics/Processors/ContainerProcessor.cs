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
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            component.ContainerData?.DestroyContainer();
            component.ContainerData = null;
        }
    }
}
