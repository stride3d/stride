// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Soft.Rendering;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.BepuPhysics.Soft.Tests;

public class DeformationModeTests : GameTestBase
{
    /// <summary>
    /// A volumetric body squashed on the floor and a cloth bent in the middle of its swing, frozen, then drawn with each deformation mode:
    /// both images must match, shadows and normal mapped lighting included. With them, a body whose mesh is under a node that turns
    /// and stretches it, one whose mesh has no tangents, one without normals either, and one made of two meshes with a material each.
    /// Once for each quality of the deformation.
    /// </summary>
    [Theory]
    [InlineData(SoftBodyDeformationQuality.Smooth)]
    [InlineData(SoftBodyDeformationQuality.Fast)]
    public static void GpuDeformationLooksLikeCpuDeformation(SoftBodyDeformationQuality quality)
    {
        var folder = Path.Combine(Path.GetTempPath(), "Stride.BepuPhysics.Soft.Tests");
        Directory.CreateDirectory(folder);
        var cpuPath = Path.Combine(folder, $"deformation-{quality}-cpu.png");
        var gpuPath = Path.Combine(folder, $"deformation-{quality}-gpu.png");

        var game = new RenderGame();
        Exception? failure = null;
        ImageTester.ComparisonStats stats = default;
        game.Script.AddTask(async () =>
        {
            try
            {
                game.Services.GetOrCreate<SoftBodyConfiguration>().DeformationQuality = quality;
                var bodies = game.SetUpScene();
                var simulation = bodies[0].Simulation!;
                for (int step = 0; step < 50; step++)
                    await simulation.AfterUpdate();
                simulation.Enabled = false;

                using (var cpu = await game.Capture(bodies, SoftBodyDeformationMode.Cpu))
                using (var stream = File.Create(cpuPath))
                    cpu.Save(stream, ImageFileType.Png);
                // What the CPU deformation leaves in the vertices is not the rest pose, the switch must not start from it
                Assert.All(bodies, body => Assert.True(DistanceToRestPose(body) > 1e-3f, $"{body.Entity.Name} is {DistanceToRestPose(body)} away from its rest pose"));
                var models = bodies.Select(b => b.Entity.Get<ModelComponent>()!.Model).ToArray();
                var cpuBounds = models.Select(model => model.BoundingBox).ToArray();
                using var gpu = await game.Capture(bodies, SoftBodyDeformationMode.Gpu);
                // The mode switches in place: what follows the model (node links, skeleton bodies) is left alone
                Assert.Equal(models, bodies.Select(b => b.Entity.Get<ModelComponent>()!.Model));
                // The CPU knows where every vertex is, the GPU bounds are worked out from the particles: they hold the same vertices
                for (int i = 0; i < bodies.Length; i++)
                    Assert.True(Holds(models[i].BoundingBox, cpuBounds[i]), $"The bounds of {bodies[i].Entity.Name} on the GPU, {models[i].BoundingBox}, leave out some of {cpuBounds[i]}");
                using (var stream = File.Create(gpuPath))
                    gpu.Save(stream, ImageFileType.Png);
                // The vertex shader deforms what is in the vertex buffer: the rest pose, in the space of the model
                Assert.All(bodies, body => Assert.Equal(0f, DistanceToRestPose(body)));

                ImageTester.CompareImage(gpu, cpuPath, out stats);
            }
            catch (Exception e)
            {
                failure = e;
            }
            game.Exit();
        });
        RunGameTest(game);
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);

        // Both modes run the same math, in another order: rounding can move an edge or a shadow texel by one pixel, nothing more
        var message = $"{stats}, see {cpuPath} and {gpuPath}";
        Assert.True(stats.TotalPixels > 0, message);
        Assert.True(stats.PixelsAtDiff3Plus <= stats.TotalPixels / 1000, message);
        Assert.True(stats.PSNR > 45.0, message);
    }

    /// <summary>
    /// The bounds of a body deformed on the GPU, which never sees its vertices, hold them whatever the particles do: a lattice
    /// pulled to six times its length, then turned and sheared as well, and a cloth on a mirrored entity.
    /// </summary>
    [Theory]
    [InlineData(SoftBodyDeformationQuality.Smooth)]
    [InlineData(SoftBodyDeformationQuality.Fast)]
    public static void GpuBoundsHoldTheVertices(SoftBodyDeformationQuality quality)
    {
        RunRendered(async game =>
        {
            game.Services.GetOrCreate<SoftBodyConfiguration>().DeformationQuality = quality;
            var root = game.SetUpStage();
            var jelly = new VolumetricSoftBodyComponent { Resolution = 4, Gravity = false };
            var jellyEntity = new Entity("Jelly") { new ModelComponent(game.Model(new SphereProceduralModel { Radius = 0.35f, Tessellation = 24 }, game.Material(new Color(255, 111, 89), normalMapped: false))), jelly };
            jellyEntity.Transform.Position = new Vector3(-0.6f, 1f, 0f);
            root.Add(jellyEntity);
            var mirrored = new ClothComponent { Gravity = false };
            var mirroredEntity = new Entity("Mirrored") { new ModelComponent(game.Model(new PlaneProceduralModel { Size = new Vector2(1f), Tessellation = new Int2(8), GenerateBackFace = true }, game.Material(new Color(61, 125, 216), normalMapped: false))), mirrored };
            mirroredEntity.Transform.Position = new Vector3(0.7f, 1f, 0f);
            mirroredEntity.Transform.Scale = new Vector3(-1f, 1f, 1f);
            root.Add(mirroredEntity);
            for (int frame = 0; frame < 4; frame++)
                await game.Script.NextFrame();

            // A square of side one, and what keeps its two faces apart
            Assert.Equal(SoftBodyDeformationMode.Gpu, mirrored.RenderModel!.Mode);
            var extent = mirrored.RenderModel.Model.BoundingBox.Extent;
            Assert.True(extent.X < 0.6f && extent.Y < 0.6f && extent.Z < 0.6f, $"The bounds of the mirrored cloth reach {extent} from their center");

            var topology = jelly.Topology!;
            bool smooth = quality == SoftBodyDeformationQuality.Smooth;
            using var deformation = new GpuDeformation(game.GraphicsDevice, topology, smooth);
            var positions = new Vector3[topology.Mesh.Positions.Length];
            var frames = new Vector3[topology.ParticleCount * 3];
            foreach (var warp in new[] { Matrix.Scaling(6f, 1f, 1f), Matrix.Scaling(6f, 1f, 0.5f) * new Matrix { M11 = 1f, M22 = 1f, M33 = 1f, M44 = 1f, M21 = 0.8f } * Matrix.RotationYawPitchRoll(0.7f, 0.4f, 0.2f) })
            {
                var particles = Array.ConvertAll(topology.RestPositions, rest => Vector3.TransformCoordinate(rest, warp));
                var bounds = deformation.Update(particles, Matrix.Identity, 0f, 0f);
                if (smooth)
                {
                    topology.ComputeParticleFrames(particles, frames);
                    for (int v = 0; v < positions.Length; v++)
                        topology.DeformSmooth(v, particles, frames, out positions[v], out _, out _);
                }
                else
                {
                    topology.Deform(particles, positions);
                }

                var vertices = BoundingBox.FromPoints(positions);
                Assert.True(Holds(bounds, vertices), $"The bounds worked out from the particles, {bounds}, leave out some of the vertices, within {vertices}");
            }
        });
    }

    /// <summary>
    /// A tessellated draw keeps the vertex buffers it was made from. A body with such a material is drawn the same by both modes,
    /// switched while it is on screen: it gets a model of its own again for the draw to be made anew, where the others keep theirs.
    /// </summary>
    [Fact]
    public static void TessellatedBodiesSwitchDeformation()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Stride.BepuPhysics.Soft.Tests");
        Directory.CreateDirectory(folder);
        var cpuPath = Path.Combine(folder, "tessellated-cpu.png");
        RunRendered(async game =>
        {
            var root = game.SetUpStage();
            var tessellated = new VolumetricSoftBodyComponent { Resolution = 4 };
            var tessellatedEntity = new Entity("Tessellated") { new ModelComponent(game.Model(new SphereProceduralModel { Radius = 0.35f, Tessellation = 16 }, game.Material(new Color(255, 111, 89), normalMapped: true, tessellated: true))), tessellated };
            tessellatedEntity.Transform.Position = new Vector3(-0.4f, 0.8f, 0f);
            root.Add(tessellatedEntity);
            var plain = new VolumetricSoftBodyComponent { Resolution = 4 };
            var plainEntity = new Entity("Plain") { new ModelComponent(game.Model(new SphereProceduralModel { Radius = 0.35f, Tessellation = 16 }, game.Material(new Color(96, 190, 120), normalMapped: true))), plain };
            plainEntity.Transform.Position = new Vector3(0.6f, 0.8f, 0f);
            root.Add(plainEntity);
            SoftBodyComponent[] bodies = [tessellated, plain];

            await game.Script.NextFrame();
            var simulation = tessellated.Simulation!;
            for (int step = 0; step < 50; step++)
                await simulation.AfterUpdate();
            simulation.Enabled = false;

            using (var cpu = await game.Capture(bodies, SoftBodyDeformationMode.Cpu))
            using (var stream = File.Create(cpuPath))
                cpu.Save(stream, ImageFileType.Png);
            var tessellatedModel = tessellatedEntity.Get<ModelComponent>()!.Model;
            var plainModel = plainEntity.Get<ModelComponent>()!.Model;

            using var gpu = await game.Capture(bodies, SoftBodyDeformationMode.Gpu);
            ImageTester.CompareImage(gpu, cpuPath, out var stats);
            var message = $"{stats}, see {cpuPath}";
            Assert.True(stats.TotalPixels > 0, message);
            Assert.True(stats.PixelsAtDiff3Plus <= stats.TotalPixels / 1000, message);
            Assert.True(stats.PSNR > 45.0, message);

            // What the renderer draws the tessellated mesh from is what its model has now, not what it had on the CPU
            var renderModels = game.SceneSystem.SceneInstance.GetProcessor<ModelRenderProcessor>().RenderModels;
            foreach (var entity in new[] { tessellatedEntity, plainEntity })
            {
                var drawn = renderModels[entity.Get<ModelComponent>()!].Meshes[0].ActiveMeshDraw.VertexBuffers;
                var current = entity.Get<ModelComponent>()!.Model.Meshes[0].Draw.VertexBuffers;
                Assert.Equal(current.Select(binding => binding.Buffer), drawn.Select(binding => binding.Buffer));
            }
            Assert.NotSame(tessellatedModel, tessellatedEntity.Get<ModelComponent>()!.Model);
            Assert.Same(plainModel, plainEntity.Get<ModelComponent>()!.Model);
        });
    }

    /// <summary>
    /// Deforming on the GPU goes through a render feature of the compositor. Without a compositor to add it to, the bodies are
    /// deformed on the CPU, with a word; with one, the feature only goes through the meshes while a body is deformed on the GPU.
    /// </summary>
    [Fact]
    public static void GpuDeformationNeedsItsRenderFeature()
    {
        RunRendered(async game =>
        {
            var warnings = new List<string>();
            void Collect(Stride.Core.Diagnostics.ILogMessage message)
            {
                if (message.Type == Stride.Core.Diagnostics.LogMessageType.Warning)
                    warnings.Add(message.Text);
            }
            Stride.Core.Diagnostics.GlobalLogger.GlobalMessageLogged += Collect;
            try
            {
                var root = game.SetUpStage();
                var compositor = game.SceneSystem.GraphicsCompositor;
                game.SceneSystem.GraphicsCompositor = new GraphicsCompositor();
                var jelly = new VolumetricSoftBodyComponent { Resolution = 3 };
                var entity = new Entity("Jelly") { new ModelComponent(game.Model(new SphereProceduralModel { Radius = 0.35f }, game.Material(new Color(255, 111, 89), normalMapped: false))), jelly };
                entity.Transform.Position = new Vector3(0f, 0.8f, 0f);
                root.Add(entity);
                for (int frame = 0; frame < 4; frame++)
                    await game.Script.NextFrame();

                Assert.Equal(SoftBodyDeformationMode.Cpu, jelly.RenderModel!.Mode);
                Assert.Single(warnings, warning => warning.Contains("compositor"));

                game.SceneSystem.GraphicsCompositor = compositor;
                for (int frame = 0; frame < 4; frame++)
                    await game.Script.NextFrame();
                var feature = compositor.RenderFeatures.OfType<MeshRenderFeature>().SelectMany(mesh => mesh.RenderFeatures).OfType<SoftBodyDeformationRenderFeature>().Single();
                Assert.Equal(SoftBodyDeformationMode.Gpu, jelly.RenderModel!.Mode);
                Assert.True(feature.Active);

                game.Services.GetOrCreate<SoftBodyConfiguration>().DeformationMode = SoftBodyDeformationMode.Cpu;
                for (int frame = 0; frame < 4; frame++)
                    await game.Script.NextFrame();
                Assert.Equal(SoftBodyDeformationMode.Cpu, jelly.RenderModel!.Mode);
                Assert.False(feature.Active);
            }
            finally
            {
                Stride.Core.Diagnostics.GlobalLogger.GlobalMessageLogged -= Collect;
            }
        });
    }

    /// <summary> Whether <paramref name="inner"/> is within <paramref name="outer"/>, give or take what rounding does </summary>
    private static bool Holds(BoundingBox outer, BoundingBox inner)
    {
        const float rounding = 1e-4f;
        var below = outer.Minimum - inner.Minimum;
        var above = inner.Maximum - outer.Maximum;
        return MathF.Max(MathF.Max(below.X, below.Y), MathF.Max(below.Z, MathF.Max(above.X, MathF.Max(above.Y, above.Z)))) <= rounding;
    }

    /// <summary> Runs <paramref name="test"/> in a game that draws at the profile deforming on the GPU needs, and rethrows what it threw </summary>
    private static void RunRendered(Func<RenderGame, Task> test)
    {
        var game = new RenderGame();
        Exception? failure = null;
        game.Script.AddTask(async () =>
        {
            try
            {
                await test(game);
            }
            catch (Exception e)
            {
                failure = e; // Thrown from a script it would only be logged, and the game would never exit
            }
            game.Exit();
        });
        RunGameTest(game);
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);
    }

    /// <summary> How far the positions, normals and tangents in the vertices of a body are from the ones of its rest pose, at most </summary>
    private static float DistanceToRestPose(SoftBodyComponent body)
    {
        var renderModel = body.RenderModel!;
        var rest = renderModel.Topology.Mesh;
        float distance = 0f;
        for (int m = 0; m < rest.Parts.Length; m++)
        {
            var declaration = renderModel.Model.Meshes[m].Draw.VertexBuffers[0].Declaration;
            var bytes = renderModel.VertexBytes(m);
            int stride = declaration.VertexStride;
            foreach (var element in declaration.EnumerateWithOffsets())
            {
                if (element.VertexElement.SemanticIndex != 0 || element.VertexElement.SemanticName is not ("POSITION" or "NORMAL" or "TANGENT"))
                    continue;

                for (int i = 0; i < rest.Parts[m].VertexCount; i++)
                {
                    int vertex = rest.Parts[m].FirstVertex + i;
                    var expected = element.VertexElement.SemanticName switch { "POSITION" => rest.Positions[vertex], "NORMAL" => rest.Normals[vertex], _ => (Vector3)rest.Tangents[vertex] };
                    var actual = MemoryMarshal.Read<Vector3>(bytes[(i * stride + element.Offset)..]);
                    distance = MathF.Max(distance, Vector3.Distance(expected, actual));
                }
            }
        }
        return distance;
    }

    private sealed class RenderGame : GameTestBase
    {
        private readonly List<IDisposable> _created = new(); // The textures and buffers of the scene, released with the game
        private Image? _capture;
        private bool _captureRequested;

        public RenderGame()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
            GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
            GraphicsDeviceManager.PreferredBackBufferHeight = 720;
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        }

        /// <summary> A lit floor seen from a camera, for the bodies of a test to be drawn on </summary>
        public TrackingCollection<Entity> SetUpStage()
        {
            ScreenShotAutomationEnabled = false;
            var compositor = GraphicsCompositorHelper.CreateDefault(true, clearColor: new Color4(0.6f, 0.7f, 0.85f, 1f), graphicsProfile: GraphicsProfile.Level_11_0);
            SceneSystem.GraphicsCompositor = compositor;
            var root = SceneSystem.SceneInstance.RootScene.Entities;
            root.Clear();

            var sun = new Entity { new LightComponent { Type = new LightDirectional { Shadow = { Enabled = true, Size = LightShadowMapSize.Large } }, Intensity = 3f } };
            sun.Transform.Rotation = Quaternion.RotationYawPitchRoll(0.6f, -0.9f, 0f);
            root.Add(sun);
            root.Add(new Entity { new LightComponent { Type = new LightAmbient(), Intensity = 0.3f } });
            var camera = new Entity { new CameraComponent { Slot = compositor.Cameras[0].ToSlotId(), NearClipPlane = 0.05f, FarClipPlane = 100f } };
            camera.Transform.Position = new Vector3(0f, 1.2f, 2.6f);
            camera.Transform.Rotation = Quaternion.RotationX(-0.3f);
            root.Add(camera);

            var floor = new Entity
            {
                new ModelComponent(Model(new PlaneProceduralModel { Size = new Vector2(8f) }, Material(new Color(150, 146, 140), normalMapped: false))),
                new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(8f, 1f, 8f), PositionLocal = new Vector3(0f, -0.5f, 0f) } } } },
            };
            root.Add(floor);
            return root;
        }

        public SoftBodyComponent[] SetUpScene()
        {
            var root = SetUpStage();

            // Volumetric: dropped onto the floor, it squashes and wobbles
            var jelly = new VolumetricSoftBodyComponent { Resolution = 4 };
            var jellyEntity = new Entity("Jelly") { new ModelComponent(Model(new SphereProceduralModel { Radius = 0.35f, Tessellation = 24 }, Material(new Color(255, 111, 89), normalMapped: true))), jelly };
            jellyEntity.Transform.Position = new Vector3(-0.55f, 0.8f, 0f);
            root.Add(jellyEntity);

            // Cloth: pinned along one edge, it swings down and bends
            var edge = new BoundingBox(new Vector3(-0.51f, -1f, -1f), new Vector3(-0.49f, 1f, 1f));
            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = edge } } };
            var plane = new PlaneProceduralModel { Size = new Vector2(1f), Tessellation = new Int2(16), Normal = NormalDirection.UpY, GenerateBackFace = true };
            // Its tangents carry a handedness of -1, as mirrored texture coordinates give them: both modes leave it to the mesh
            var clothModel = Track(VertexLayoutTests.WithLayout(this, plane, VertexLayoutTests.Float3, VertexLayoutTests.Float3, VertexLayoutTests.Float4, out _));
            clothModel.Materials.Clear();
            clothModel.Materials.Add(new MaterialInstance(Material(new Color(61, 125, 216), normalMapped: true)));
            var clothEntity = new Entity("Cloth") { new ModelComponent(clothModel), cloth };
            clothEntity.Transform.Position = new Vector3(0.75f, 1.3f, -0.3f);
            root.Add(clothEntity);

            // Volumetric, its mesh under a node that turns and stretches it, as the meshes of an imported model can be:
            // what is in its vertices is in the space of the node, the rest pose in the space of the model
            var nodedModel = Model(new SphereProceduralModel { Radius = 0.25f, Tessellation = 24 }, Material(new Color(96, 190, 120), normalMapped: true));
            nodedModel.Skeleton = new Skeleton
            {
                Nodes =
                [
                    new ModelNodeDefinition { Name = "Root", ParentIndex = -1, Flags = ModelNodeFlags.Default, Transform = new TransformTRS { Rotation = Quaternion.Identity, Scale = Vector3.One } },
                    new ModelNodeDefinition { Name = "Mesh", ParentIndex = 0, Flags = ModelNodeFlags.Default, Transform = new TransformTRS { Rotation = Quaternion.RotationYawPitchRoll(0.8f, 0.5f, 0.3f), Scale = new Vector3(1f, 1.4f, 0.8f) } },
                ],
            };
            nodedModel.Meshes[0].NodeIndex = 1;
            var noded = new VolumetricSoftBodyComponent { Resolution = 4 };
            var nodedEntity = new Entity("Noded") { new ModelComponent(nodedModel), noded };
            nodedEntity.Transform.Position = new Vector3(0.05f, 0.8f, 0.9f);
            root.Add(nodedEntity);

            // Volumetric, without tangents in its mesh, as a model imported from a file without them
            var bareModel = Track(VertexLayoutTests.WithLayout(this, new SphereProceduralModel { Radius = 0.3f, Tessellation = 16 }, VertexLayoutTests.Float3, VertexLayoutTests.Float3, VertexLayoutTests.Absent, out _));
            bareModel.Materials.Clear();
            bareModel.Materials.Add(new MaterialInstance(Material(new Color(230, 200, 80), normalMapped: false)));
            var bare = new VolumetricSoftBodyComponent { Resolution = 4 };
            var bareEntity = new Entity("Bare") { new ModelComponent(bareModel), bare };
            bareEntity.Transform.Position = new Vector3(-1.3f, 0.8f, 0.3f);
            root.Add(bareEntity);

            // Volumetric, without normals either
            var flatModel = Track(VertexLayoutTests.WithLayout(this, new SphereProceduralModel { Radius = 0.25f, Tessellation = 16 }, VertexLayoutTests.Float3, VertexLayoutTests.Absent, VertexLayoutTests.Absent, out _));
            flatModel.Materials.Clear();
            flatModel.Materials.Add(new MaterialInstance(Material(new Color(170, 120, 210), normalMapped: false)));
            var flat = new VolumetricSoftBodyComponent { Resolution = 4 };
            var flatEntity = new Entity("Flat") { new ModelComponent(flatModel), flat };
            flatEntity.Transform.Position = new Vector3(-0.7f, 0.8f, 1.1f);
            root.Add(flatEntity);

            // Volumetric, made of two meshes with a material each: the vertices of the second one come after the first one's
            var twoModel = Model(new SphereProceduralModel { Radius = 0.2f, Tessellation = 16 }, Material(new Color(240, 240, 240), normalMapped: true));
            twoModel.Materials.Add(new MaterialInstance(Material(new Color(40, 40, 40), normalMapped: false)));
            twoModel.Skeleton = new Skeleton
            {
                Nodes =
                [
                    new ModelNodeDefinition { Name = "Root", ParentIndex = -1, Flags = ModelNodeFlags.Default, Transform = new TransformTRS { Rotation = Quaternion.Identity, Scale = Vector3.One } },
                    new ModelNodeDefinition { Name = "Second", ParentIndex = 0, Flags = ModelNodeFlags.Default, Transform = new TransformTRS { Position = new Vector3(0.25f, 0.1f, 0f), Rotation = Quaternion.Identity, Scale = Vector3.One } },
                ],
            };
            twoModel.Meshes.Add(new Mesh(twoModel.Meshes[0]) { NodeIndex = 1, MaterialIndex = 1 });
            var two = new VolumetricSoftBodyComponent { Resolution = 5 };
            var twoEntity = new Entity("Two meshes") { new ModelComponent(twoModel), two };
            twoEntity.Transform.Position = new Vector3(0.9f, 0.8f, 0.9f);
            root.Add(twoEntity);

            return [jelly, cloth, noded, bare, flat, two];
        }

        /// <summary> Draws the frozen bodies with <paramref name="mode"/> once every effect is compiled, and reads the back buffer </summary>
        public async Task<Image> Capture(IReadOnlyList<SoftBodyComponent> bodies, SoftBodyDeformationMode mode)
        {
            Services.GetOrCreate<SoftBodyConfiguration>().DeformationMode = mode;
            for (int frame = 0; frame < 600 && (frame < 4 || IsCompiling()); frame++)
                await Script.NextFrame();
            Assert.False(IsCompiling(), "Effects still compiling");
            Assert.All(bodies, body => Assert.Equal(mode, body.RenderModel?.Mode));
            // Volumetric bodies follow the settings, a cloth has nothing to smooth
            var quality = Services.GetOrCreate<SoftBodyConfiguration>().DeformationQuality;
            Assert.All(bodies, body => Assert.Equal(body is VolumetricSoftBodyComponent ? quality : SoftBodyDeformationQuality.Fast, body.RenderModel?.Quality));
            // And on the GPU, their meshes ask for the vertex shader of that quality
            Assert.All(bodies, body =>
            {
                var shader = body.RenderModel!.Model.Meshes[0].Parameters.Get(StrideEffectBaseKeys.ExtensionPostVertexStageShader) as Stride.Shaders.ShaderClassSource;
                var expected = mode != SoftBodyDeformationMode.Gpu ? null
                    : body is not VolumetricSoftBodyComponent ? "SoftBodySurfaceDeformation"
                    : quality == SoftBodyDeformationQuality.Smooth ? "SoftBodySmoothLatticeDeformation" : "SoftBodyLatticeDeformation";
                Assert.Equal(expected, shader?.ClassName);
            });

            _captureRequested = true;
            while (_capture is null)
                await Script.NextFrame();
            var image = _capture;
            _capture = null;
            return image;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            if (_captureRequested)
            {
                _captureRequested = false;
                _capture = GraphicsDevice.Presenter.BackBuffer.GetDataAsImage(GraphicsContext.CommandList);
            }
        }

        private bool IsCompiling()
        {
            foreach (var feature in SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>())
            {
                var renderEffects = feature.RenderData.GetData(feature.RenderEffectKey);
                var slotCount = feature.EffectPermutationSlotCount;
                foreach (var renderObject in feature.RenderObjects)
                {
                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        var renderEffect = renderEffects[renderObject.StaticObjectNode * slotCount + slot];
                        if (renderEffect is { PendingEffect: not null } || renderEffect?.State == RenderEffectState.Compiling)
                            return true;
                    }
                }
            }
            return false;
        }

        public Model Model(PrimitiveProceduralModelBase shape, Material material)
        {
            var model = Track(new ProceduralModelDescriptor(shape).GenerateModel(Services));
            model.Materials.Clear();
            model.Materials.Add(new MaterialInstance(material));
            return model;
        }

        /// <summary> Has the buffers of <paramref name="model"/> released with the game </summary>
        private Model Track(Model model)
        {
            foreach (var mesh in model.Meshes)
            {
                _created.Add(mesh.Draw.IndexBuffer.Buffer);
                foreach (var vertexBuffer in mesh.Draw.VertexBuffers)
                    _created.Add(vertexBuffer.Buffer);
            }
            return model;
        }

        protected override void Destroy()
        {
            foreach (var created in _created)
                created.Dispose();
            _created.Clear();
            base.Destroy();
        }

        /// <summary> A glossy material, with a normal map of bumps for the tangents to matter, tessellated when asked </summary>
        public Material Material(Color color, bool normalMapped, bool tessellated = false)
        {
            var descriptor = new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color.ToColor4())),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.6f)),
                    Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0f)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature { Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial() },
                },
            };
            if (normalMapped)
                descriptor.Attributes.Surface = new MaterialNormalMapFeature(new ComputeTextureColor(Bumps()));
            if (tessellated)
                descriptor.Attributes.Tessellation = new MaterialTessellationFlatFeature();
            return Stride.Rendering.Material.New(GraphicsDevice, descriptor);
        }

        private Texture Bumps()
        {
            const int size = 64;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Slopes of sin(u) sin(v) bumps, four per side
                    float u = x * MathUtil.TwoPi * 4f / size, v = y * MathUtil.TwoPi * 4f / size;
                    var normal = Vector3.Normalize(new Vector3(-0.5f * MathF.Cos(u) * MathF.Sin(v), -0.5f * MathF.Sin(u) * MathF.Cos(v), 1f));
                    pixels[y * size + x] = new Color(normal * 0.5f + new Vector3(0.5f));
                }
            }
            var texture = Texture.New2D(GraphicsDevice, size, size, PixelFormat.R8G8B8A8_UNorm, pixels);
            _created.Add(texture);
            return texture;
        }
    }
}
