using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.BepuPhysics.Systems;

public class ConstraintProcessor : EntityProcessor<ConstraintComponentBase>
{
    private BepuConfiguration _bepuConfiguration = new();

    public ConstraintProcessor()
    {
        Order = SystemsOrderHelper.ORDER_OF_CONSTRAINT_P;
    }

    protected override void OnSystemAdd()
    {
        ServicesHelper.LoadBepuServices(Services, out _bepuConfiguration, out _, out _);
    }

    protected override void OnEntityComponentAdding(Entity entity, ConstraintComponentBase component, ConstraintComponentBase data)
    {
        base.OnEntityComponentAdding(entity, component, data);
        component.CreateProcessorData(_bepuConfiguration).RebuildConstraint();
    }
    protected override void OnEntityComponentRemoved(Entity entity, ConstraintComponentBase component, ConstraintComponentBase data)
    {
        base.OnEntityComponentRemoved(entity, component, data);
        component.UntypedConstraintData?.DestroyConstraint();
        component.RemoveDataRef();
    }
}
