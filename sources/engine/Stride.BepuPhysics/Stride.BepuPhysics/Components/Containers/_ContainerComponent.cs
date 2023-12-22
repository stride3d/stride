using System.Diagnostics;
using BepuPhysics.Collidables;
using Silk.NET.OpenGL;
using Stride.BepuPhysics.Components.Colliders;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Collisions;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;
using static BepuPhysics.Collidables.CompoundBuilder;
using Mesh = BepuPhysics.Collidables.Mesh;

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
        private ushort _colliderFilterByDistanceId = 0; //0 => Feature not enabled
        private ushort _colliderFilterByDistanceX = 0; //collision occur if deltaX > 1
        private ushort _colliderFilterByDistanceY = 0; //collision occur if deltaY > 1
        private ushort _colliderFilterByDistanceZ = 0; //collision occur if deltaZ > 1

        private bool _ignoreGlobalGravity = false;

        private IContactEventHandler? _contactEventHandler = null;

        internal List<ContainerComponent> ChildsContainerComponent { get; } = new();
        internal IServiceRegistry Services { get; set; }

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        [DataMemberIgnore]
        internal ContainerData? ContainerData { get; set; }

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

        [DataMemberIgnore]
        public BepuSimulation? Simulation
        {
            get => ContainerData?.BepuSimulation;
        }

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
        public ushort ColliderFilterByDistanceId
        {
            get => _colliderFilterByDistanceId;
            set
            {
                _colliderFilterByDistanceId = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public ushort ColliderFilterByDistanceX
        {
            get => _colliderFilterByDistanceX;
            set
            {
                _colliderFilterByDistanceX = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public ushort ColliderFilterByDistanceY
        {
            get => _colliderFilterByDistanceY;
            set
            {
                _colliderFilterByDistanceY = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public ushort ColliderFilterByDistanceZ
        {
            get => _colliderFilterByDistanceZ;
            set
            {
                _colliderFilterByDistanceX = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }

        public bool IgnoreGlobalGravity
        {
            get => _ignoreGlobalGravity;
            set
            {
                if (_ignoreGlobalGravity == value)
                    return;

                _ignoreGlobalGravity = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }

        public Vector3 CenterOfMass { get; internal set; } = new Vector3();



#warning toLeftHanded not implemented yet and doesn't need to ?

        public List<BodyShapeData> GetShapeData(bool toLeftHanded = true)
        {
            var shapes = new List<BodyShapeData>();
            if (ContainerData == null)
                return default;

            AddShapeData(shapes, ContainerData.ShapeIndex, toLeftHanded);

            return shapes;
        }
        private void AddShapeData(List<BodyShapeData> shapes, TypedIndex typeIndex, bool toLeftHanded = true)
        {
            var shapeType = typeIndex.Type;
            var shapeIndex = typeIndex.Index;

            switch (shapeType)
            {
                case 0:
                    var sphere = Simulation.Simulation.Shapes.GetShape<Sphere>(shapeIndex);
                    shapes.Add(GetBodyShapeData(GetSphereVerts(sphere, toLeftHanded)));
                    break;
                case 1:
                    var capsule = Simulation.Simulation.Shapes.GetShape<Capsule>(shapeIndex);
                    shapes.Add(GetBodyShapeData(GetCapsuleVerts(capsule, toLeftHanded)));
                    break;
                case 2:
                    var box = Simulation.Simulation.Shapes.GetShape<Box>(shapeIndex);
                    shapes.Add(GetBodyShapeData(GetBoxVerts(box, toLeftHanded)));
                    break;
                case 3:
#warning TODO : shapeData.Transform = objectTransform;
                    var triangle = Simulation.Simulation.Shapes.GetShape<Triangle>(shapeIndex);
                    var a = Vector3.Transform(triangle.A.ToStrideVector(), Entity.Transform.WorldMatrix).XYZ();
                    var b = Vector3.Transform(triangle.A.ToStrideVector(), Entity.Transform.WorldMatrix).XYZ();
                    var c = Vector3.Transform(triangle.A.ToStrideVector(), Entity.Transform.WorldMatrix).XYZ();
                    var shapeData = new BodyShapeData() { Points = new List<Vector3>() { a, b, c }, Indices = new List<int>() { 0, 1, 2 } };
                    shapes.Add(shapeData);
                    break;
                case 4:
                    var cyliner = Simulation.Simulation.Shapes.GetShape<Cylinder>(shapeIndex);
                    shapes.Add(GetBodyShapeData(GetCylinderVerts(cyliner, toLeftHanded)));
                    break;
#warning Same for 5,6,8
                case 5:
                    var convex = Simulation.Simulation.Shapes.GetShape<ConvexHull>(shapeIndex);
                    shapes.Add(GetConvexData(convex, toLeftHanded));
                    break;
                case 6:
                    var compound = Simulation.Simulation.Shapes.GetShape<Compound>(shapeIndex);
                    shapes.AddRange(GetCompoundData(compound, toLeftHanded));
                    break;
                case 7:
                    throw new NotImplementedException("BigCompounds are not implemented.");
                case 8:
                    var mesh = Simulation.Simulation.Shapes.GetShape<Mesh>(shapeIndex);
                    shapes.Add(GetMeshData(mesh, toLeftHanded));
                    break;
            }
        }

        private BodyShapeData GetBodyShapeData(GeometricMeshData<VertexPositionNormalTexture> meshData, bool toLeftHanded = true)
        {
            BodyShapeData shapeData = new BodyShapeData();

            // Transform box points
            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                shapeData.Points.Add(meshData.Vertices[i].Position);
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
        private BodyShapeData GetConvexData(ConvexHull convex, bool toLeftHanded = true)
        {
            //use Strides shape data
            var entities = new List<Entity>();
            entities.Add(Entity);
            ConvexHullColliderComponent hullComponent = null;
            do
            {
                var ent = entities.First();
                entities.RemoveAt(0);

                hullComponent = ent.Get<ConvexHullColliderComponent>();
                if (hullComponent != null)
                    break;
                entities.AddRange(ent.GetChildren());
            }
            while (entities.Count != 0);

            if (hullComponent == null)
                throw new Exception("A convex that doesn't have a convexHullCollider ?");

            var shape = (ConvexHullColliderShapeDesc)hullComponent.Hull.Descriptions[0];

            BodyShapeData shapeData = new BodyShapeData();

            for (int i = 0; i < shape.ConvexHulls[0][0].Count; i++)
            {
                shapeData.Points.Add(shape.ConvexHulls[0][0][i]);
            }

            for (int i = 0; i < shape.ConvexHullsIndices[0][0].Count; i += 3)
            {
                shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i]);
                shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i + 2]); // NOTE: Reversed winding to create left handed input
                shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i + 1]);
            }

            return shapeData;
        }
        private List<BodyShapeData> GetCompoundData(Compound compound, bool toLeftHanded = true)
        {
            var shapeData = new List<BodyShapeData>();

            for (int i = 0; i < compound.ChildCount; i++)
            {
                var child = compound.GetChild(i);
                var startI = shapeData.Count;
                AddShapeData(shapeData, child.ShapeIndex);

                for (int ii = startI; ii < shapeData.Count; ii++)
                {
                    var translatedData = shapeData[ii].Points.Select(e => Vector3.Transform(e, child.LocalOrientation.ToStrideQuaternion()) + child.LocalPosition.ToStrideVector()).ToArray();
                    shapeData[ii].Points.Clear();
                    shapeData[ii].Points.AddRange(translatedData);
                }
            }
            return shapeData;
        }
        private BodyShapeData GetMeshData(Mesh mesh, bool toLeftHanded = true)
        {
            var meshContainer = (IMeshContainerComponent)this;

            if (meshContainer == null)
                throw new Exception("a mesh must be inside a MeshContainer");

            if (meshContainer.Model == null)
                return default;

            var game = Services.GetService<IGame>();
            BodyShapeData shapeData = GetMeshData(meshContainer.Model, game);

            for (int i = 0; i < shapeData.Indices.Count; i += 3)
            {
                // NOTE: Reversed winding to create left handed input
                (shapeData.Indices[i + 1], shapeData.Indices[i + 2]) = (shapeData.Indices[i + 2], shapeData.Indices[i + 1]);
            }

            return shapeData;
        }


        private GeometricMeshData<VertexPositionNormalTexture> GetBoxVerts(Box box, bool toLeftHanded = true)
        {
            var boxDescription = new BoxColliderShapeDesc()
            {
                Size = new Vector3(box.Width, box.Height, box.Length)
            };
            return GeometricPrimitive.Cube.New(boxDescription.Size, toLeftHanded: true);
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetCapsuleVerts(Capsule capsule, bool toLeftHanded = true)
        {
            var capsuleDescription = new CapsuleColliderShapeDesc()
            {
                Length = capsule.Length,
                Radius = capsule.Radius
            };
            return GeometricPrimitive.Capsule.New(capsuleDescription.Length, capsuleDescription.Radius, 8, toLeftHanded: true);
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetSphereVerts(Sphere sphere, bool toLeftHanded = true)
        {
            var sphereDescription = new SphereColliderShapeDesc()
            {
                Radius = sphere.Radius
            };
            return GeometricPrimitive.Sphere.New(sphereDescription.Radius, 16, toLeftHanded: true);
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetCylinderVerts(Cylinder cylinder, bool toLeftHanded = true)
        {
            var cylinderDescription = new CylinderColliderShapeDesc()
            {
                Height = cylinder.Length,
                Radius = cylinder.Radius
            };
            return GeometricPrimitive.Cylinder.New(cylinderDescription.Height, cylinderDescription.Radius, 32, toLeftHanded: true);
        }


        private static unsafe BodyShapeData GetMeshData(Model model, IGame game)
        {
            BodyShapeData bodyData = new BodyShapeData();
            int totalVertices = 0, totalIndices = 0;
            foreach (var meshData in model.Meshes)
            {
                totalVertices += meshData.Draw.VertexBuffers[0].Count;
                totalIndices += meshData.Draw.IndexBuffer.Count;
            }

            foreach (var meshData in model.Meshes)
            {
                var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
                var iBuffer = meshData.Draw.IndexBuffer.Buffer;
                byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList);
                byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList);

                if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
                {
                    // returns empty lists if there is an issue
                    return bodyData;
                }

                int vertMappingStart = bodyData.Points.Count;

                fixed (byte* bytePtr = verticesBytes)
                {
                    var vBindings = meshData.Draw.VertexBuffers[0];
                    int count = vBindings.Count;
                    int stride = vBindings.Declaration.VertexStride;
                    for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                    {
                        var pos = *(Vector3*)(bytePtr + vHead);

                        bodyData.Points.Add(pos);
                    }
                }

                fixed (byte* bytePtr = indicesBytes)
                {
                    if (meshData.Draw.IndexBuffer.Is32Bit)
                    {
                        foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                        {
                            bodyData.Indices.Add(vertMappingStart + i);
                        }
                    }
                    else
                    {
                        foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                        {
                            bodyData.Indices.Add(vertMappingStart + i);
                        }
                    }
                }
            }

            return bodyData;
        }

    }
}
