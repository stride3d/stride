using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using BepuPhysicIntegrationTest.Integration.Components.ConstraintsV2;

namespace BepuPhysicIntegrationTest.Integration.Components.ConstraintsV2
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessorV2), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - ConstraintV2")]
    public class BallSocketConstraintComponentV2 : ConstraintComponentV2
    {

        internal BallSocket _bepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

        public Vector3 LocalOffsetA { get => _bepuConstraint.LocalOffsetA.ToStrideVector(); set => _bepuConstraint.LocalOffsetA = value.ToNumericVector(); }
        public Vector3 LocalOffsetB { get => _bepuConstraint.LocalOffsetB.ToStrideVector(); set => _bepuConstraint.LocalOffsetB = value.ToNumericVector(); }
        public SpringSettings SpringSettings { get => _bepuConstraint.SpringSettings; set => _bepuConstraint.SpringSettings = value; }


    }
}
