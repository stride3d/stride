using System;
using System.Linq;
using BepuPhysics;
using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.CharacterConstraints;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.BepuPhysics.Processors
{
    public class ConstraintProcessor : EntityProcessor<ConstraintComponent>
    {
        private BepuConfiguration _bepuConfig = new();

        public ConstraintProcessor()
        {
            Order = 10020;
        }

        protected override void OnSystemAdd()
        {
            _bepuConfig = Services.GetService<BepuConfiguration>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ConstraintComponent component, [NotNull] ConstraintComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            component.ConstraintData = new(component, _bepuConfig.BepuSimulations[0]); //TODO : get Index from bodies
            component.ConstraintData.BuildConstraint();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ConstraintComponent component, [NotNull] ConstraintComponent data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.ConstraintData?.DestroyConstraint();
            component.ConstraintData = null;
        }
    }

    internal class ConstraintData
    {
        internal ConstraintComponent ConstraintComponent { get; }
        internal BepuSimulation BepuSimulation { get; }

        internal ConstraintHandle CHandle { get; set; } = new(-1);


        public bool Exist => CHandle.Value != -1 && BepuSimulation.Simulation.Solver.ConstraintExists(CHandle);

        public ConstraintData(ConstraintComponent constraintComponent, BepuSimulation bepuSimulation)
        {
            ConstraintComponent = constraintComponent;
            BepuSimulation = bepuSimulation;
        }

        internal void BuildConstraint()
        {
#pragma warning disable CS8602 
            var bodies = new Span<BodyHandle>(ConstraintComponent.Bodies.Where(e => e.ContainerData != null).Select(e => e.ContainerData.BHandle).ToArray());
#pragma warning restore CS8602

            if (Exist)
                DestroyConstraint();

            if (!ConstraintComponent.Enabled)
                return;

            switch (ConstraintComponent) //maybe add a IConstraintDescription to ConstraintComponent and use it instead of that switch of hell
            {
                case AngularAxisGearMotorConstraintComponent _aagmcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _aagmcc._bepuConstraint);
                    break;
                case AngularAxisMotorConstraintComponent _aamcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _aamcc._bepuConstraint);
                    break;
                case AngularHingeConstraintComponent _ahcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _ahcc._bepuConstraint);
                    break;
                case AngularMotorConstraintComponent _amcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _amcc._bepuConstraint);
                    break;
                case AngularServoConstraintComponent _ascc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _ascc._bepuConstraint);
                    break;
                case AngularSwivelHingeConstraintComponent _ashcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _ashcc._bepuConstraint);
                    break;
                case AreaConstraintComponent _acc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _acc._bepuConstraint);
                    break;
                case BallSocketConstraintComponent _bscc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _bscc._bepuConstraint);
                    break;
                case BallSocketMotorConstraintComponent _bsmcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _bsmcc._bepuConstraint);
                    break;
                case BallSocketServoConstraintComponent _bsscc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _bsscc._bepuConstraint);
                    break;
                case CenterDistanceConstraintComponent _cdcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _cdcc._bepuConstraint);
                    break;
                case CenterDistanceLimitConstraintComponent _cdlcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _cdlcc._bepuConstraint);
                    break;
                case DistanceLimitConstraintComponent _dlcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _dlcc._bepuConstraint);
                    break;
                case DistanceServoConstraintComponent _dscc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _dscc._bepuConstraint);
                    break;
                case HingeConstraintComponent _hcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _hcc._bepuConstraint);
                    break;
                case LinearAxisLimitConstraintComponent _lalcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _lalcc._bepuConstraint);
                    break;
                case LinearAxisMotorConstraintComponent _lamcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _lamcc._bepuConstraint);
                    break;
                case LinearAxisServoConstraintComponent _lascc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _lascc._bepuConstraint);
                    break;
                case OneBodyAngularMotorConstraintComponent _obamcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _obamcc._bepuConstraint);
                    break;
                case OneBodyAngularServoConstraintComponent _obascc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _obascc._bepuConstraint);
                    break;
                case OneBodyLinearMotorConstraintComponent _oblmcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _oblmcc._bepuConstraint);
                    break;
                case OneBodyLinearServoConstraintComponent _oblscc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _oblscc._bepuConstraint);
                    break;
                case PointOnLineServoConstraintComponent _polscc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _polscc._bepuConstraint);
                    break;
                case SwingLimitConstraintComponent _slcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _slcc._bepuConstraint);
                    break;
                case SwivelHingeConstraintComponent _shcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _shcc._bepuConstraint);
                    break;
                case TwistLimitConstraintComponent _tlcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _tlcc._bepuConstraint);
                    break;
                case TwistMotorConstraintComponent _tmcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _tmcc._bepuConstraint);
                    break;
                case TwistServoConstraintComponent _tscc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _tscc._bepuConstraint);
                    break;
                case VolumeConstraintComponent _vcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _vcc._bepuConstraint);
                    break;
                case WeldConstraintComponent _wcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _wcc._bepuConstraint);
                    break;
                case StaticCharacterConstraint _scc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _scc._bepuConstraint);
                    break;
                default:
                    break;
            }
        }
        internal void DestroyConstraint()
        {
            if (Exist)
            {
                BepuSimulation.Simulation.Solver.Remove(CHandle);
            }
        }
    }

}
