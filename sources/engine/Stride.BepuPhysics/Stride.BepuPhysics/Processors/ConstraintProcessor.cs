using BepuPhysics;
using BepuPhysics.Constraints;
using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Configurations;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.BepuPhysics.Processors
{
    public class ConstraintProcessor : EntityProcessor<BaseConstraintComponent>
    {
        private BepuConfiguration _bepuConfiguration = new();

        public ConstraintProcessor()
        {
            Order = 10020;
        }

        protected override void OnSystemAdd()
        {
            _bepuConfiguration = Services.GetService<BepuConfiguration>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] BaseConstraintComponent component, [NotNull] BaseConstraintComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            var constraint = component.CreateProcessorData(_bepuConfiguration);
            constraint.BuildConstraint();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] BaseConstraintComponent component, [NotNull] BaseConstraintComponent data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.UntypedConstraintData?.DestroyConstraint();
        }
    }

    internal abstract class BaseConstraintData
    {
        internal abstract void BuildConstraint();
        internal abstract void DestroyConstraint();
        internal abstract void TryUpdateDescription();
    }

    internal sealed class ConstraintData<T> : BaseConstraintData where T : unmanaged, IConstraintDescription<T>
    {
        private readonly ConstraintComponent<T> _constraintComponent;
        private readonly BepuConfiguration _bepuConfig;
        private ConstraintHandle _cHandle = new(-1);
        private BepuSimulation _bepuSimulation;

        public ConstraintData(ConstraintComponent<T> constraintComponent, BepuConfiguration bepuConfig)
        {
            _constraintComponent = constraintComponent;
            _bepuConfig = bepuConfig;
            _bepuSimulation = _bepuConfig.BepuSimulations[_constraintComponent.Bodies[0].SimulationIndex];
        }

        internal override void BuildConstraint()
        {
            if (_cHandle.Value != -1)
                DestroyConstraint();

            if (_constraintComponent.Bodies.Count == 0 || !_constraintComponent.Enabled)
                return;

            var simIndex = _constraintComponent.Bodies[0].SimulationIndex;
            _bepuSimulation = _bepuConfig.BepuSimulations[simIndex];
            foreach (var component in _constraintComponent.Bodies)
            {
#warning maybe send a warning, like the missing camera notification in the engine, instead of exception
                if (component.SimulationIndex != simIndex)
                    throw new Exception("A constraint between object with different SimulationIndex is not possible");
            }

            Span<BodyHandle> bodies = stackalloc BodyHandle[_constraintComponent.Bodies.Count];
            int count = 0;

            foreach (var component1 in _constraintComponent.Bodies)
            {
                if (component1.ContainerData != null)
                    bodies[count++] = component1.ContainerData.BHandle;
            }

            Span<BodyHandle> validBodies = bodies[..count];

            _cHandle = _bepuSimulation.Simulation.Solver.Add(validBodies, _constraintComponent.BepuConstraint);
        }

        internal override void DestroyConstraint()
        {
            if (_cHandle.Value != -1)
            {
                _bepuSimulation.Simulation.Solver.Remove(_cHandle);
                _cHandle = new(-1);
            }

            _constraintComponent.ConstraintData = null;
        }

        internal override void TryUpdateDescription()
        {
            if (_cHandle.Value != -1)
            {
                _bepuSimulation.Simulation.Solver.ApplyDescription(_cHandle, _constraintComponent.BepuConstraint);
            }
        }
    }
}
