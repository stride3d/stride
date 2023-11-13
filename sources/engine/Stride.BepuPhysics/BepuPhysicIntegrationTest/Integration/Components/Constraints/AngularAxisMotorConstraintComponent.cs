using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Constraints
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public class AngularAxisMotorConstraintComponent : TwoBodyConstraintComponent
    {
        public Vector3 LocalAxisA { get; set; } = new Vector3(0, 1, 0);
        public float TargetVelocity { get; set; } = 1f;
        public MotorSettings Settings { get; set; } = new(100, 10);

    }
}
