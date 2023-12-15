using BepuPhysics.Collidables;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.Collisions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics;
using Stride.Physics;
using BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]

    public abstract class ContainerComponent : EntityComponent
    {
        private int _simulationIndex = 0;
        private float _springFrequency = 30;
        private float _springDampingRatio = 3;
        private float _frictionCoefficient = 1f;
        private float _maximumRecoveryVelocity = 1000;
        private byte _colliderGroupMask = byte.MaxValue; //1111 1111 => collide with everything
        private bool _ignoreGravity = false;

        private IContactEventHandler? _contactEventHandler = null;

        internal List<ContainerComponent> ChildsContainerComponent { get; } = new();

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        [DataMemberIgnore]
        internal ContainerData? ContainerData { get; set; }


        public int SimulationIndex
        {
            get
            {
                return _simulationIndex;
            }
            set
            {
                ContainerData?.DestroyContainer();
                _simulationIndex = value;
                ContainerData?.RebuildContainer();
            }
        }

        [DataMemberIgnore]
        public BepuSimulation? Simulation
        {
            get => ContainerData?.BepuSimulation;
        }

        public float SpringFrequency
        {
            get
            {
                return _springFrequency;
            }
            set
            {
                _springFrequency = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public float SpringDampingRatio
        {
            get
            {
                return _springDampingRatio;
            }
            set
            {
                _springDampingRatio = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public float FrictionCoefficient
        {
            get => _frictionCoefficient;
            set
            {
                _frictionCoefficient = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public float MaximumRecoveryVelocity
        {
            get => _maximumRecoveryVelocity;
            set
            {
                _maximumRecoveryVelocity = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public byte ColliderGroupMask
        {
            get => _colliderGroupMask;
            set
            {
                _colliderGroupMask = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public bool IgnoreGravity
        {
            get => _ignoreGravity;
            set
            {
                if (_ignoreGravity == value)
                    return;

                _ignoreGravity = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }

        public Vector3 CenterOfMass { get; internal set; } = new Vector3();       

        [DataMemberIgnore]
        public IContactEventHandler? ContactEventHandler
        {
            get => _contactEventHandler;
            set
            {
                if (ContainerData?.IsRegistered() == true)
                    ContainerData?.UnregisterContact();

                _contactEventHandler = value;
                ContainerData?.RegisterContact();
            }
		}

		private BodyReference GetRef()
		{
			if (ContainerData == null)
				throw new Exception("");

			return ContainerData.BepuSimulation.Simulation.Bodies[ContainerData.BHandle];
		}

		public BodyShapeData GetShapeData()
		{
			var shape = ContainerData.ShapeIndex.Type;
			var index = ContainerData.ShapeIndex.Index;

			GeometricMeshData<VertexPositionNormalTexture> meshData;
            BodyShapeData shapeData = new BodyShapeData();

			switch (shape)
			{
				case 0:
					var sphere = Simulation.Simulation.Shapes.GetShape<Sphere>(index);
					meshData = GetSphereVerts(sphere);
                    shapeData = GetBodyShapeData(meshData, Entity.Transform.WorldMatrix);
					break;
				case 1:
					var capsule = Simulation.Simulation.Shapes.GetShape<Capsule>(index);
					meshData = GetCapsuleVerts(capsule);
					shapeData = GetBodyShapeData(meshData, Entity.Transform.WorldMatrix);
					break;
				case 2:
					var box = Simulation.Simulation.Shapes.GetShape<Box>(index);
					meshData = GetBoxVerts(box);
					shapeData = GetBodyShapeData(meshData, Entity.Transform.WorldMatrix);
					break;
				case 3:
					var triangle = Simulation.Simulation.Shapes.GetShape<Triangle>(index);
					break;
				case 4:
					var cyliner = Simulation.Simulation.Shapes.GetShape<Cylinder>(index);
					meshData = GetCylinderVerts(cyliner);
					shapeData = GetBodyShapeData(meshData, Entity.Transform.WorldMatrix);
					break;
				case 5:
					var convex = Simulation.Simulation.Shapes.GetShape<ConvexHull>(index);
					shapeData = GetConvexData(convex);
					break;
                case 6:
                    var compound = Simulation.Simulation.Shapes.GetShape<Compound>(index);
					shapeData = GetCompoundData(compound);
					break;
				case 7:
					throw new NotImplementedException("BigCompounds are not implemented.");
					break;
				case 8:
					var mesh = Simulation.Simulation.Shapes.GetShape<Mesh>(index);
					shapeData = GetMeshData(mesh, Entity.Transform.WorldMatrix);
					break;

			}

            return shapeData;
		}

		public BodyShapeData GetShapeData(TypedIndex typeIndex)
		{
			var shape = typeIndex.Type;
			var index = typeIndex.Index;

			GeometricMeshData<VertexPositionNormalTexture> meshData;
			BodyShapeData shapeData = new BodyShapeData();

			switch (shape)
			{
				case 0:
					var sphere = Simulation.Simulation.Shapes.GetShape<Sphere>(index);
					meshData = GetSphereVerts(sphere);
					shapeData = GetBodyShapeData(meshData, Entity.Transform.WorldMatrix);
					break;
				case 1:
					var capsule = Simulation.Simulation.Shapes.GetShape<Capsule>(index);
					meshData = GetCapsuleVerts(capsule);
					shapeData = GetBodyShapeData(meshData, Entity.Transform.WorldMatrix);
					break;
				case 2:
					var box = Simulation.Simulation.Shapes.GetShape<Box>(index);
					meshData = GetBoxVerts(box);
					shapeData = GetBodyShapeData(meshData, Entity.Transform.WorldMatrix);
					break;
				case 3:
					var triangle = Simulation.Simulation.Shapes.GetShape<Triangle>(index);
					break;
				case 4:
					var cyliner = Simulation.Simulation.Shapes.GetShape<Cylinder>(index);
					meshData = GetCylinderVerts(cyliner);
					shapeData = GetBodyShapeData(meshData, Entity.Transform.WorldMatrix);
					break;
				case 5:
					var convex = Simulation.Simulation.Shapes.GetShape<ConvexHull>(index);
					shapeData = GetConvexData(convex);
					break;
				case 6:
					var compound = Simulation.Simulation.Shapes.GetShape<Compound>(index);
					shapeData = GetCompoundData(compound);
					break;
				case 7:
					throw new NotImplementedException("BigCompounds are not implemented.");
					break;
				case 8:
					var mesh = Simulation.Simulation.Shapes.GetShape<Mesh>(index);
					shapeData = GetMeshData(mesh, Entity.Transform.WorldMatrix);
					break;
			}

			return shapeData;
		}

		private BodyShapeData GetBodyShapeData(GeometricMeshData<VertexPositionNormalTexture> meshData, Matrix objectTransform)
		{
            BodyShapeData shapeData = new BodyShapeData();

			// Transform box points
			for (int i = 0; i < meshData.Vertices.Length; i++)
			{
				VertexPositionNormalTexture point = meshData.Vertices[i];
				point.Position = Vector3.Transform(point.Position, objectTransform).XYZ();
				shapeData.Points.Add(point.Position);
			}

			if (meshData.IsLeftHanded)
			{
				// Copy indices with offset applied
				for (int i = 0; i < meshData.Indices.Length; i += 3)
				{
					shapeData.Indices.Add(meshData.Indices[i]);
					shapeData.Indices.Add(meshData.Indices[i + 2]);
					shapeData.Indices.Add(meshData.Indices[i + 1]);
				}
			}
			else
			{
				// Copy indices with offset applied
				for (int i = 0; i < meshData.Indices.Length; i++)
				{
					shapeData.Indices.Add(meshData.Indices[i]);
				}
			}

            return shapeData;
		}

		private GeometricMeshData<VertexPositionNormalTexture> GetBoxVerts(Box box)
		{
			var boxDescription = new BoxColliderShapeDesc()
			{
				//Size = new Vector3(box.Width, box.Height, box.Length)
			};
			return GeometricPrimitive.Cube.New(boxDescription.Size, toLeftHanded: true);
		}
		private GeometricMeshData<VertexPositionNormalTexture> GetCapsuleVerts(Capsule capsule)
		{
			var capsuleDescription = new CapsuleColliderShapeDesc()
			{
				//Length = capsule.Length,
				//Radius = capsule.Radius
			};
			return GeometricPrimitive.Capsule.New(capsuleDescription.Length, capsuleDescription.Radius, 8, toLeftHanded: true);
		}
		private GeometricMeshData<VertexPositionNormalTexture> GetSphereVerts(Sphere sphere)
		{
			var sphereDescription = new SphereColliderShapeDesc()
			{
				//Radius = sphere.Radius
			};
			return GeometricPrimitive.Sphere.New(sphereDescription.Radius, 16, toLeftHanded: true);
		}
		private GeometricMeshData<VertexPositionNormalTexture> GetCylinderVerts(Cylinder cylinder)
		{
			var cylinderDescription = new CylinderColliderShapeDesc()
			{
				//Height = cylinder.Length,
				//Radius = cylinder.Radius
			};
			return GeometricPrimitive.Cylinder.New(cylinderDescription.Height, cylinderDescription.Radius, 32, toLeftHanded: true);
		}
		private BodyShapeData GetConvexData(ConvexHull convex)
		{
            BodyShapeData shapeData = new BodyShapeData();

            for(int i = 0; i < convex.FaceToVertexIndicesStart.Length; i++)
            {
                // will need to get the points from the convex hull
                convex.GetPoint(convex.FaceToVertexIndicesStart[i], out var point);
                shapeData.Points.Add(point.ToStrideVector());
			}

			return shapeData;
		}
        private BodyShapeData GetCompoundData(Compound compound)
        {
			BodyShapeData shapeData = new BodyShapeData();

			for (int i = 0; i < compound.ChildCount; i++)
            {
				var child = compound.GetChild(i);
				var childShapeData = GetShapeData(child.ShapeIndex);

				shapeData.Points.AddRange(childShapeData.Points);
				shapeData.Indices.AddRange(childShapeData.Indices);
			}

			return shapeData;
		}
		private BodyShapeData GetMeshData(Mesh mesh, Matrix objectTransform)
		{
			BodyShapeData shapeData = new BodyShapeData();

			for(int i = 0; i < mesh.Triangles.Length; i++)
			{
				var triangle = mesh.Triangles[i];
				shapeData.Points.Add(Vector3.Transform(triangle.A.ToStrideVector(), objectTransform).XYZ());
				shapeData.Points.Add(triangle.B.ToStrideVector());
				shapeData.Points.Add(triangle.C.ToStrideVector());

				shapeData.Indices.Add(i * 0);
				shapeData.Indices.Add(i * 1);
				shapeData.Indices.Add(i * 2);
			}

			return shapeData;
		}

	}
}
