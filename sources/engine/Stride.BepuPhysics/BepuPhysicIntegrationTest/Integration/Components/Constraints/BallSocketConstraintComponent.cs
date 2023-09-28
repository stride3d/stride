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
    public class BallSocketConstraintComponent : ConstraintComponent
    {
        public Vector3 LocalOffsetA { get; set; } = new Vector3(0, -1f, 0);
        public Vector3 LocalOffsetB { get; set; } = new Vector3(0, 1f, 0);
        public SpringSettings SpringSettings { get; set; } = new SpringSettings(30, 5);

    }
}
