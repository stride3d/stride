using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IBodyContainer : IContainer
    {
        public bool Kinematic { get; set; }
        public float SleepThreshold { get; set; }
        public byte MinimumTimestepCountUnderThreshold { get; set; }


        public bool Awake { get; set; }
        public Vector3 LinearVelocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public BodyInertia BodyInertia { get; set; }
        public float SpeculativeMargin { get; set; }
        public ContinuousDetection ContinuousDetection { get; set; }

        public void ApplyImpulse(Vector3 impulse, Vector3 impulseOffset);
        public void ApplyAngularImpulse(Vector3 impulse);
        public void ApplyLinearImpulse(Vector3 impulse);
    }
}