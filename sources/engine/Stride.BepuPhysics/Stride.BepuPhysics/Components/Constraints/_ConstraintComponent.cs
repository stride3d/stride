using BepuPhysics.Constraints;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Processors;
using Stride.Core;

namespace Stride.BepuPhysics.Components.Constraints
{

    public abstract class ConstraintComponent<T> : ConstraintComponentBase where T : unmanaged, IConstraintDescription<T>
    {
        internal T BepuConstraint;

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Set through the processor when it calls <see cref="CreateProcessorData"/>.
        /// </summary>
        [DataMemberIgnore]
        internal ConstraintData<T>? ConstraintData { get; set; }

        internal override void RemoveDataRef()
        {
            ConstraintData = null;
        }

        internal override ConstraintDataBase? UntypedConstraintData => ConstraintData;

        internal override ConstraintDataBase CreateProcessorData(BepuConfiguration bepuConfiguration) => ConstraintData = new(this, bepuConfiguration);
    }
}
