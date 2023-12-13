using System.Collections.ObjectModel;
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
        private bool _enabled = true;

        public ObservableCollection<BodyContainerComponent> Bodies { get; set; }
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

        public BaseConstraintComponent()
        {
            Bodies = new();
            Bodies.CollectionChanged += (s, e) => UntypedConstraintData?.RebuildConstraint();
        }

        internal abstract void RemoveDataRef();

        internal abstract BaseConstraintData? UntypedConstraintData { get; }

        internal abstract BaseConstraintData CreateProcessorData(BepuConfiguration bepuConfiguration);
    }

    //TODO : choose observable or update that implementation.
    public sealed class BodyContainerList : List<BodyContainerComponent>
    {
        private Action _editedCallBack { get; }
        public BodyContainerList(Action editedCallBack)
        {
            _editedCallBack = editedCallBack;
        }


        public new void Add(BodyContainerComponent item)
        {
            base.Add(item);
            _editedCallBack?.Invoke();
        }

        public new void Remove(BodyContainerComponent item)
        {
            base.Remove(item);
            _editedCallBack?.Invoke();
        }

        public new void AddRange(IEnumerable<BodyContainerComponent> collection)
        {
            base.AddRange(collection);
            _editedCallBack?.Invoke();
        }

        public new void Clear()
        {
            base.Clear();
            _editedCallBack?.Invoke();
        }
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

        internal override void RemoveDataRef()
        {
            ConstraintData = null;
        }

        internal override BaseConstraintData? UntypedConstraintData => ConstraintData;

        internal override BaseConstraintData CreateProcessorData(BepuConfiguration bepuConfiguration) => ConstraintData = new(this, bepuConfiguration);
    }
}
