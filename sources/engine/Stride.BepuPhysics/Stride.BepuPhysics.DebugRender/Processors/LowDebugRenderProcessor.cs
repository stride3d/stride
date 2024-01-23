using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.DebugRender.Components;
using Stride.BepuPhysics.DebugRender.Effects;
using Stride.BepuPhysics.DebugRender.Effects.RenderFeatures;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Systems;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;
using cMesh = BepuPhysics.Collidables.Mesh;
using static Stride.Rendering.Shadows.LightDirectionalShadowMapRenderer;
using SharpFont;
using System.ComponentModel;

namespace Stride.BepuPhysics.DebugRender.Processors
{

#warning dunno if we should keep it like that, it's a debug render that ignore stride and just look at bepu.

    public class LowDebugRenderProcessor : EntityProcessor<LowDebugRenderComponent>
    {
        private IGame _game = null!;
        private SceneSystem _sceneSystem = null!;
        private InputManager _input = null!;
        private BepuSimulation _sim = null!;
        private SinglePassWireframeRenderFeature _wireframeRenderFeature = null!;
        private VisibilityGroup _visibilityGroup = null!;
        private List<WireFrameRenderObject> _wireFrameList = new();

        public LowDebugRenderProcessor()
        {
            Order = SystemsOrderHelper.ORDER_OF_DEBUG_P;
        }

        protected override void OnSystemAdd()
        {
            ServicesHelper.LoadBepuServices(Services);
            _game = Services.GetService<IGame>();
            _sceneSystem = Services.GetService<SceneSystem>();
            _input = Services.GetService<InputManager>();
#warning Sim0
            _sim = Services.GetService<BepuConfiguration>().BepuSimulations[0];

            if (_sceneSystem.GraphicsCompositor.RenderFeatures.OfType<SinglePassWireframeRenderFeature>().FirstOrDefault() is { } wireframeFeature)
            {
                _wireframeRenderFeature = wireframeFeature;
            }
            else
            {
                _wireframeRenderFeature = new();
                _sceneSystem.GraphicsCompositor.RenderFeatures.Add(_wireframeRenderFeature);
            }

            _visibilityGroup = _sceneSystem.SceneInstance.VisibilityGroups.First();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] LowDebugRenderComponent component, [NotNull] LowDebugRenderComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
        }

        public override void Draw(RenderContext context)
        {
            base.Draw(context);

            if (_input.IsKeyPressed(Keys.F11))
            {
                while (_wireFrameList.Any())
                {
                    var wireFrame = _wireFrameList[0];
                    _wireFrameList.RemoveAt(0);
                    _visibilityGroup.RenderObjects.Remove(wireFrame);
                    wireFrame.Dispose();
                }

                var count = _sim.Simulation.Bodies.CountBodies();
                for (int i = 0; i < count; i++)
                {
                    var body = _sim.Simulation.Bodies[new(i)];
                    var bodyPos = body.Pose.Position.ToStrideVector();

                    GetBasicMeshBuffers(body, out var Buffers);

                    WireFrameRenderObject[] wireframes = new WireFrameRenderObject[Buffers.Count];
                    for (int i2 = 0; i2 < Buffers.Count; i2++)
                    {
                        var data = Buffers[i2];
                        wireframes[i2] = WireFrameRenderObject.New(_game.GraphicsDevice, data.Indices, data.Vertices);
                        wireframes[i2].Color = Color.Red;
                        Matrix.Transformation(in Vector3.One, in Quaternion.Identity, in Vector3.Zero, out wireframes[i2].ContainerBaseMatrix);
                        Matrix.Transformation(in Vector3.One, in Quaternion.Identity, ref bodyPos, out wireframes[i2].WorldMatrix);
                        _visibilityGroup.RenderObjects.Add(wireframes[i2]);
                    }
                }
            }
        }

        public void GetBasicMeshBuffers(BodyReference bodyRef, out List<BasicMeshBuffers> shapes)
        {
            shapes = new();
            var shapeIndex = bodyRef.Collidable.Shape;
            AddShapeData(shapes, shapeIndex);
        }
        private void AddShapeData(List<BasicMeshBuffers> shapes, TypedIndex typeIndex)
        {
            var shapeType = typeIndex.Type;
            var shapeIndex = typeIndex.Index;

            switch (shapeType)
            {
                case 0:
                    var sphere = _sim.Simulation.Shapes.GetShape<Sphere>(shapeIndex);
                    shapes.Add(GetBodyShapeData(GetSphereVerts(sphere)));
                    break;
                case 1:
                    var capsule = _sim.Simulation.Shapes.GetShape<Capsule>(shapeIndex);
                    shapes.Add(GetBodyShapeData(GetCapsuleVerts(capsule)));
                    break;
                case 2:
                    var box = _sim.Simulation.Shapes.GetShape<Box>(shapeIndex);
                    shapes.Add(GetBodyShapeData(GetBoxVerts(box)));
                    break;
                case 3:
                    var triangle = _sim.Simulation.Shapes.GetShape<Triangle>(shapeIndex);
                    var a = new VertexPosition3(triangle.A.ToStrideVector());
                    var b = new VertexPosition3(triangle.B.ToStrideVector());
                    var c = new VertexPosition3(triangle.C.ToStrideVector());
                    var shapeData = new BasicMeshBuffers() { Vertices = [a, b, c], Indices = [0, 1, 2] };
                    shapes.Add(shapeData);
                    break;
                case 4:
                    var cyliner = _sim.Simulation.Shapes.GetShape<Cylinder>(shapeIndex);
                    shapes.Add(GetBodyShapeData(GetCylinderVerts(cyliner)));
                    break;
                //case 5:
                //    var convex = _sim.Simulation.Shapes.GetShape<ConvexHull>(shapeIndex);
                //    shapes.Add(GetConvexHullData(convex, toLeftHanded));
                //    break;
                case 6:
                    var compound = _sim.Simulation.Shapes.GetShape<Compound>(shapeIndex);
                    shapes.AddRange(GetCompoundData(compound));
                    break;
                //case 7:
                //throw new NotImplementedException("BigCompounds are not implemented.");
                case 8:
                    var mesh = _sim.Simulation.Shapes.GetShape<cMesh>(shapeIndex);
                    //shapes.Add(GetMeshData(mesh, toLeftHanded));
                    break;
            }
        }

        private BasicMeshBuffers GetBodyShapeData(GeometricMeshData<VertexPositionNormalTexture> meshData)
        {
            BasicMeshBuffers shapeData = new BasicMeshBuffers();

            // Transform box points
            shapeData.Vertices = new VertexPosition3[meshData.Vertices.Length];
            shapeData.Indices = new int[meshData.Indices.Length];

            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                shapeData.Vertices[i] = new(meshData.Vertices[i].Position);
            }

            // Copy indices with offset applied
            for (int i = 0; i < meshData.Indices.Length; i++)
            {
                shapeData.Indices[i] = (meshData.Indices[i]);
            }

            return shapeData;
        }
        private List<BasicMeshBuffers> GetCompoundData(Compound compound)
        {
            var shapeData = new List<BasicMeshBuffers>();

            for (int i = 0; i < compound.ChildCount; i++)
            {
                var child = compound.GetChild(i);
                var startI = shapeData.Count;
                AddShapeData(shapeData, child.ShapeIndex);

                for (int ii = startI; ii < shapeData.Count; ii++)
                {
                    for (int iii = 0; iii < shapeData[ii].Vertices.Length; iii++)
                    {
                        shapeData[ii].Vertices[iii].Position = Vector3.Transform(shapeData[ii].Vertices[iii].Position, child.LocalOrientation.ToStrideQuaternion()) + child.LocalPosition.ToStrideVector();
                    }
                }
            }
            return shapeData;
        }

#warning convexHull & model not handled
        //        private BasicMeshBuffers GetConvexHullData(ConvexHull convex, bool toLeftHanded = true)
        //        {
        //            Vector3 scale = Vector3.One;
        //            //use Strides shape data
        //            var entities = new List<Entity>();
        //            entities.Add(Entity);
        //            ConvexHullColliderComponent hullComponent = null;
        //            do
        //            {
        //                var ent = entities.First();
        //                entities.RemoveAt(0);

        //                hullComponent = ent.Get<ConvexHullColliderComponent>();
        //                if (hullComponent == null)
        //                    entities.AddRange(ent.GetChildren());
        //                else
        //                    scale = ent.Transform.Scale;
        //            }
        //            while (entities.Count != 0);

        //            if (hullComponent == null)
        //                throw new Exception("A convex that doesn't have a convexHullCollider ?");

        //            var shape = (ConvexHullColliderShapeDesc)hullComponent.Hull.Descriptions[0];
        //            var test = shape.LocalOffset;
        //            BodyShapeData shapeData = new BodyShapeData();

        //            for (int i = 0; i < shape.ConvexHulls[0][0].Count; i++)
        //            {
        //                shapeData.Points.Add(shape.ConvexHulls[0][0][i] * scale);
        //                shapeData.Normals.Add(Vector3.Zero);//Edit code to get normals
        //#warning scaling & normals
        //            }

        //            for (int i = 0; i < shape.ConvexHullsIndices[0][0].Count; i += 3)
        //            {
        //                shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i]);
        //                shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i + 2]); // NOTE: Reversed winding to create left handed input
        //                shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i + 1]);
        //            }

        //            return shapeData;
        //        }

        private GeometricMeshData<VertexPositionNormalTexture> GetBoxVerts(Box box)
        {
            var boxDescription = new BoxColliderShapeDesc()
            {
                Size = new Vector3(box.Width, box.Height, box.Length)
            };
            return GeometricPrimitive.Cube.New(boxDescription.Size, toLeftHanded: true);
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetCapsuleVerts(Capsule capsule)
        {
            var capsuleDescription = new CapsuleColliderShapeDesc()
            {
                Length = capsule.Length,
                Radius = capsule.Radius
            };
            return GeometricPrimitive.Capsule.New(capsuleDescription.Length, capsuleDescription.Radius, 8, toLeftHanded: true);
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetSphereVerts(Sphere sphere)
        {
            var sphereDescription = new SphereColliderShapeDesc()
            {
                Radius = sphere.Radius
            };
            return GeometricPrimitive.Sphere.New(sphereDescription.Radius, 16, toLeftHanded: true);
        }
        private GeometricMeshData<VertexPositionNormalTexture> GetCylinderVerts(Cylinder cylinder)
        {
            var cylinderDescription = new CylinderColliderShapeDesc()
            {
                Height = cylinder.Length,
                Radius = cylinder.Radius
            };
            return GeometricPrimitive.Cylinder.New(cylinderDescription.Height, cylinderDescription.Radius, 32, toLeftHanded: true);
        }

    }
}
