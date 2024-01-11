using Stride.BepuPhysics.Components.Containers.Interfaces;
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
    public abstract class ConstraintComponentBase : SyncScript
    {
        private bool _enabled = true;
        private readonly IBodyContainer?[] _bodies;

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                UntypedConstraintData?.RebuildConstraint();
            }
        }

        public ReadOnlySpan<IBodyContainer?> Bodies => _bodies;

        protected ConstraintComponentBase(int bodies) => _bodies = new IBodyContainer?[bodies];

        protected IBodyContainer? this[int i]
        {
            get => _bodies[i];
            set
            {
                _bodies[i] = value;
                UntypedConstraintData?.RebuildConstraint();
            }
        }

        public override void Update()
        {
            if (UntypedConstraintData?.Exist == false)
                UntypedConstraintData.RebuildConstraint();
        }

        internal abstract void RemoveDataRef();

        internal abstract ConstraintDataBase? UntypedConstraintData { get; }

        internal abstract ConstraintDataBase CreateProcessorData(BepuConfiguration bepuConfiguration);
    }
}
