using BepuPhysics;
using BepuPhysics.Constraints;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Constraints
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    [AllowMultipleComponents]
    public abstract class BaseConstraintComponent : EntityComponent
    {
        public List<BodyContainerComponent> Bodies { get; set; } = new(); //TODO implement list with updates

        private bool _update = true;

        public bool Enabled
        {
            get
            {
                return _update;
            }
            set
            {
                _update = value;
                UntypedConstraintData?.BuildConstraint();
            }
        }

        internal abstract BaseConstraintData? UntypedConstraintData { get; }

        internal abstract BaseConstraintData CreateProcessorData(BepuConfiguration bepuConfiguration);
    }

    public abstract class ConstraintComponent<T> : BaseConstraintComponent where T : unmanaged, IConstraintDescription<T>
    {
        internal T BepuConstraint;

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Set through the processor when it calls <see cref="CreateProcessorData"/>.
        /// </summary>
        [DataMemberIgnore]
        internal ConstraintData<T>? ConstraintData { get; set; }

        internal override BaseConstraintData? UntypedConstraintData => ConstraintData;

        internal override BaseConstraintData CreateProcessorData(BepuConfiguration bepuConfiguration) => ConstraintData = new(this, bepuConfiguration);
    }
}
