// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.Engine;

namespace Stride.BepuPhysics.Systems;

public class ConstraintProcessor : EntityProcessor<ConstraintComponentBase>
{
    private BepuConfiguration _bepuConfiguration = null!;

    public ConstraintProcessor()
    {
        Order = SystemsOrderHelper.ORDER_OF_CONSTRAINT_P;
    }

    protected override void OnSystemAdd()
    {
        _bepuConfiguration = Services.GetOrCreate<BepuConfiguration>();
    }

    protected override void OnEntityComponentAdding(Entity entity, ConstraintComponentBase component, ConstraintComponentBase data)
    {
        component.Activate(_bepuConfiguration);
    }
    protected override void OnEntityComponentRemoved(Entity entity, ConstraintComponentBase component, ConstraintComponentBase data)
    {
        component.Deactivate();
    }
}
