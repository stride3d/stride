using BepuPhysics.Constraints;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Constraints
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public sealed class OneBodyAngularServoConstraintComponent : OneBodyConstraintComponent<OneBodyAngularServo>
    {
        public OneBodyAngularServoConstraintComponent() => BepuConstraint = new()
        {
            ServoSettings = new ServoSettings(),
            SpringSettings = new SpringSettings(30, 5)
        };

        public Quaternion TargetOrientation
        {
            get
            {
                return BepuConstraint.TargetOrientation.ToStrideQuaternion();
            }
            set
            {
                BepuConstraint.TargetOrientation = value.ToNumericQuaternion();
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float SpringFrequency
        {
            get
            {
                return BepuConstraint.SpringSettings.Frequency;
            }
            set
            {
                BepuConstraint.SpringSettings.Frequency = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float SpringDampingRatio
        {
            get
            {
                return BepuConstraint.SpringSettings.DampingRatio;
            }
            set
            {
                BepuConstraint.SpringSettings.DampingRatio = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float ServoMaximumSpeed
        {
            get
            {
                return BepuConstraint.ServoSettings.MaximumSpeed;
            }
            set
            {
                BepuConstraint.ServoSettings.MaximumSpeed = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float ServoBaseSpeed
        {
            get
            {
                return BepuConstraint.ServoSettings.BaseSpeed;
            }
            set
            {
                BepuConstraint.ServoSettings.BaseSpeed = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float ServoMaximumForce
        {
            get
            {
                return BepuConstraint.ServoSettings.MaximumForce;
            }
            set
            {
                BepuConstraint.ServoSettings.MaximumForce = value;
                ConstraintData?.TryUpdateDescription();
            }
        }
    }

}
