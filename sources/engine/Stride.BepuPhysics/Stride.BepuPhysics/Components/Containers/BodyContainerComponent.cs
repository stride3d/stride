using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class BodyContainerComponent : ContainerComponent
    {
        private bool _kinematic = false;
        private float _sleepThreshold = 0.01f;
        private byte _minimumTimestepCountUnderThreshold = 32;

        public bool Kinematic
        {
            get => _kinematic;
            set
            {
                _kinematic = value;
                ContainerData?.TryUpdateContainer();
            }
        }
        public float SleepThreshold
        {
            get => _sleepThreshold;
            set
            {
                _sleepThreshold = value;
                ContainerData?.TryUpdateContainer();
            }
        }
        public byte MinimumTimestepCountUnderThreshold
        {
            get => _minimumTimestepCountUnderThreshold;
            set
            {
                _minimumTimestepCountUnderThreshold = value;
                ContainerData?.TryUpdateContainer();
            }
        }

#warning This will be deleted !!!
        public BodyReference? GetPhysicBody()
        {
            return ContainerData?.BepuSimulation.Simulation.Bodies[ContainerData.BHandle];
        }

        private BodyReference GetRef()
        {
            if (ContainerData == null)
                throw new Exception("");

            return ContainerData.BepuSimulation.Simulation.Bodies[ContainerData.BHandle];
        }

        [DataMemberIgnore]
        public bool Awake
        {
            get => GetRef().Awake;
            set
            {
                var bodyRef = GetRef();
                bodyRef.Awake = value;
            }
        }

        [DataMemberIgnore]
        public Vector3 LinearVelocity
        {
            get => GetRef().Velocity.Linear.ToStrideVector();
            set
            {
                var bodyRef = GetRef();
                bodyRef.Velocity.Linear = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Vector3 AngularVelocity
        {
            get => GetRef().Velocity.Angular.ToStrideVector();
            set
            {
                var bodyRef = GetRef();
                bodyRef.Velocity.Angular = value.ToNumericVector();
            }
        }

        [DataMemberIgnore]
        public Vector3 Position
        {
            get => GetRef().Pose.Position.ToStrideVector();
            set
            {
                var bodyRef = GetRef();
                bodyRef.Pose.Position = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Quaternion Orientation
        {
            get => GetRef().Pose.Orientation.ToStrideQuaternion();
            set
            {
                var bodyRef = GetRef();
                bodyRef.Pose.Orientation = value.ToNumericQuaternion();
            }
        }

        public void ApplyImpulse(Vector3 impulse, Vector3 impulseOffset)
        {
            GetRef().ApplyImpulse(impulse.ToNumericVector(), impulseOffset.ToNumericVector());
        }
        public void ApplyAngularImpulse(Vector3 impulse)
        {
            GetRef().ApplyAngularImpulse(impulse.ToNumericVector());
        }
        public void ApplyImpulse(Vector3 impulse)
        {
            GetRef().ApplyLinearImpulse(impulse.ToNumericVector());
        }
        public void UpdateInertia(BodyInertia inertia)
        {
            GetRef().LocalInertia = inertia;
		}

        public GeometricMeshData<VertexPositionNormalTexture> GetShapeData()
        {
			var bodyRef = GetRef();
            var shape = bodyRef.Collidable.Shape;
            var type = shape.Type;
            switch(type)
            {
                case 0:
					var sphere = Simulation.Simulation.Shapes.GetShape<Sphere>(shape.Index);
                    return GetSphereVerts(sphere);
					break;
				case 1:
					var capsule = Simulation.Simulation.Shapes.GetShape<Capsule>(shape.Index);
                    return GetCapsuleVerts(capsule);
					break;
				case 2:
					var box = Simulation.Simulation.Shapes.GetShape<Box>(shape.Index);
                    return GetBoxVerts(box);
					break;
				case 3:
					var triangle = Simulation.Simulation.Shapes.GetShape<Triangle>(shape.Index);
					break;
				case 4:
					var cyliner = Simulation.Simulation.Shapes.GetShape<Cylinder>(shape.Index);
                    return GetCylinderVerts(cyliner);
					break;
				case 5:
					var convex = Simulation.Simulation.Shapes.GetShape<ConvexHull>(shape.Index);
					break;
			}
            return null;
		}
        private GeometricMeshData<VertexPositionNormalTexture> GetBoxVerts(Box box)
        {
            var boxDescription = new BoxColliderShapeDesc()
            {

            };
            return GeometricPrimitive.Cube.New(boxDescription.Size, toLeftHanded: true);
            //box.
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetCapsuleVerts(Capsule capsule)
        {
            var capsuleDescription = new CapsuleColliderShapeDesc()
            {

			};
            return GeometricPrimitive.Capsule.New(capsuleDescription.Length, capsuleDescription.Radius, 8, toLeftHanded: true);
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetSphereVerts(Sphere sphere)
        {
            var sphereDescription = new SphereColliderShapeDesc()
            {
            };
            return GeometricPrimitive.Sphere.New(sphereDescription.Radius, 16, toLeftHanded: true);
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetCylinderVerts(Cylinder cylinder)
        {
			var cylinderDescription = new CylinderColliderShapeDesc()
            {
			};
			return GeometricPrimitive.Cylinder.New(cylinderDescription.Height, cylinderDescription.Radius, 32, toLeftHanded: true);
		}
		private void GetConvexVerts(ConvexHull convex)
		{
			var test = convex.Points;
		}

	}
}
