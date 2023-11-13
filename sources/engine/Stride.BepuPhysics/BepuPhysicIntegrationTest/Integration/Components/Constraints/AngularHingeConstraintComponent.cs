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
    public class AngularHingeConstraintComponent : TwoBodyConstraintComponent
    {
        public Vector3 LocalAxisA { get; set; } = new Vector3(0, -1f, 0);
        public Vector3 LocalAxisB { get; set; } = new Vector3(0, 1f, 0);
        public SpringSettings Settings { get; set; } = new(30, 5);

    }
}
