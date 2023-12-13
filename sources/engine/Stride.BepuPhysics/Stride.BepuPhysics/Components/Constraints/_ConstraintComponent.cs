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
    public abstract class BaseConstraintComponent : SyncScript
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

        public BaseConstraintComponent()
        {
            Bodies.OnEditCallBack = () => UntypedConstraintData?.RebuildConstraint();
        }

        public override void Update()
        {
            if (UntypedConstraintData?.Exist != true)
                UntypedConstraintData?.RebuildConstraint();
        }

        internal abstract void RemoveDataRef();

        internal abstract BaseConstraintData? UntypedConstraintData { get; }

        internal abstract BaseConstraintData CreateProcessorData(BepuConfiguration bepuConfiguration);

       
    }

    //TODO : maybe replace by stride impl
    [DataContract]
    public sealed class BodyContainerList : List<BodyContainerComponent>
    {
        public Action? OnEditCallBack { get; internal set; }

        public new void Add(BodyContainerComponent item)
        {
            base.Add(item);
            OnEditCallBack?.Invoke();
        }
        public new void Remove(BodyContainerComponent item)
        {
            base.Remove(item);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveAll(Predicate<BodyContainerComponent> match)
        {
            base.RemoveAll(match);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
            OnEditCallBack?.Invoke();
        }
        public new void AddRange(IEnumerable<BodyContainerComponent> collection)
        {
            base.AddRange(collection);
            OnEditCallBack?.Invoke();
        }
        public new void Clear()
        {
            base.Clear();
            OnEditCallBack?.Invoke();
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
