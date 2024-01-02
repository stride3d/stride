using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Configurations;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.BepuPhysics.Processors
{
    public class ConstraintProcessor : EntityProcessor<ConstraintComponentBase>
    {
        private BepuConfiguration _bepuConfiguration = new();

        public ConstraintProcessor()
        {
            Order = 10100;
        }

        protected override void OnSystemAdd()
        {
            _bepuConfiguration = Services.GetService<BepuConfiguration>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ConstraintComponentBase component, [NotNull] ConstraintComponentBase data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            component.CreateProcessorData(_bepuConfiguration).RebuildConstraint();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ConstraintComponentBase component, [NotNull] ConstraintComponentBase data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.UntypedConstraintData?.DestroyConstraint();
            component.RemoveDataRef();
        }
    }
}
