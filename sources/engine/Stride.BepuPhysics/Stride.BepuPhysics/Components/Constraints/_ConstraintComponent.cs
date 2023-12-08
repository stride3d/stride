using System.Collections.Generic;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Constraints
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    [AllowMultipleComponents]

    public abstract class ConstraintComponent : EntityComponent
    {
        private bool _update = true;

        public List<BodyContainerComponent> Bodies { get; set; } = new(); //TODO implement list with updates

        public bool Enabled
        {
            get
            {
                return _update;
            }
            set
            {
                _update = value;
                if (ConstraintData != null)
                {
                    ConstraintData?.BuildConstraint();
                }
            }
        }

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        [DataMemberIgnore]
        internal ConstraintData? ConstraintData { get; set; }
    }
}
