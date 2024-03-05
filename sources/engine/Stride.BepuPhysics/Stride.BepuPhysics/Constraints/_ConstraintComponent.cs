using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;

namespace Stride.BepuPhysics.Constraints;

public abstract class ConstraintComponent<T> : ConstraintComponentBase where T : unmanaged, IConstraintDescription<T>
{
    internal T BepuConstraint;

    /// <summary> Bridge with Bepu, set through the processor when it calls <see cref="CreateProcessorData"/>. </summary>
    [DataMemberIgnore]
    internal ConstraintData<T>? ConstraintData { get; set; }

    internal override void RemoveDataRef()
    {
        ConstraintData = null;
    }

    internal override ConstraintDataBase? UntypedConstraintData => ConstraintData;

    internal override ConstraintDataBase CreateProcessorData(BepuConfiguration bepuConfiguration) => ConstraintData = new(this, bepuConfiguration);
    protected ConstraintComponent(int bodies) : base(bodies) { }
}
