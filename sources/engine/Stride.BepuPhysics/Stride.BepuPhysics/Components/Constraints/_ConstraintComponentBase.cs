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
        public BodyContainerList Bodies { get; set; } = new();

        public ConstraintComponentBase()
        {
            Bodies.OnEditCallBack = () => UntypedConstraintData?.RebuildConstraint();
        }

        public override void Update()
        {
            if (UntypedConstraintData?.Exist != true)
                UntypedConstraintData?.RebuildConstraint();
        }

        internal abstract void RemoveDataRef();

        internal abstract ConstraintDataBase? UntypedConstraintData { get; }

        internal abstract ConstraintDataBase CreateProcessorData(BepuConfiguration bepuConfiguration);


    }
}
