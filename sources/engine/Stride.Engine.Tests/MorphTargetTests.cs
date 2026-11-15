// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Animations;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Shadows;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Renders a skinned model with morph targets: the bind pose, morph weights alone, morphs with skinning, and a model
    /// scaled to zero. Each deformation path has its own gold images. Last, checks
    /// that a model outside every view is not deformed, is deformed in the first frame it is visible again, and is
    /// deformed when only a shadow view collects it.
    /// </summary>
    public class MorphTargetTests : EngineTestBase
    {
        private const int DonutCount = 5;
        private const float DonutSpacing = 3.0f;

        private static readonly MeshDeformationOverride VertexShaderPath = new(MeshDeformationMode.VertexShader);
        private static readonly MeshDeformationOverride ComputeSingleInstancePath = new(MeshDeformationMode.Compute, DeformationThreadGroup.X32Y1);
        private static readonly MeshDeformationOverride ComputeBatchedPath = new(MeshDeformationMode.Compute, DeformationThreadGroup.X8Y16);

        private readonly List<Entity> donuts = new();
        // Frames at which to count the models no view collected, and the counts.
        private readonly List<int> countFrames = new();
        private readonly Dictionary<int, int> uncollectedModelCounts = new();
        private bool testLifecycle;
        private bool testBatchGrowth;
        private Stride.Graphics.Buffer outputBeforeGrowth;
        private string testVertexLayout;
        private readonly HashSet<int> lifecycleCheckFrames = new();
        private readonly HashSet<(Stride.Graphics.Buffer Buffer, int Offset)> outputSlots = new();
        private int lifecycleChecksRun;

        public MorphTargetTests()
        {
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
            // The test package compiles effects for Level_9_3, which has no compute shaders.
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;

            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();
            EnableDirectionalShadows();

            var model = Content.Load<Model>("MorphDonut Model");
            bool hasNormals = testVertexLayout != "PositionOnly";
            bool hasTangents = hasNormals && testVertexLayout != "NoTangent";
            if (testVertexLayout != null)
            {
                foreach (var mesh in model.Meshes)
                {
                    var binding = Assert.Single(mesh.Draw.VertexBuffers);
                    var elements = binding.Declaration.EnumerateWithOffsets().ToArray();
                    var indices = elements.Single(element => element.VertexElement.SemanticName == "BLENDINDICES");
                    int originalIndexBytes = indices.VertexElement.Format.SizeInBytes;
                    int indexBytes = testVertexLayout switch
                    {
                        "Indices16" => 8,
                        "Indices8" => 4,
                        _ => originalIndexBytes,
                    };
                    int indexSizeChange = indexBytes - originalIndexBytes;
                    int stride = binding.Stride + indexSizeChange;
                    const int prefixBytes = 64;
                    var source = binding.Buffer.GetData<byte>(GraphicsContext.CommandList);
                    var vertices = new byte[prefixBytes + binding.Count * stride];
                    Array.Fill(vertices, (byte)0xCD, 0, prefixBytes);
                    for (int vertex = 0; vertex < binding.Count; vertex++)
                    {
                        var sourceVertex = source.AsSpan(binding.Offset + vertex * binding.Stride, binding.Stride);
                        var destinationVertex = vertices.AsSpan(prefixBytes + vertex * stride, stride);
                        if (indexSizeChange == 0)
                        {
                            sourceVertex.CopyTo(destinationVertex);
                            continue;
                        }
                        sourceVertex[..indices.Offset].CopyTo(destinationVertex);
                        for (int influence = 0; influence < 4; influence++)
                        {
                            ushort index = originalIndexBytes == 8
                                ? BinaryPrimitives.ReadUInt16LittleEndian(sourceVertex[(indices.Offset + influence * 2)..])
                                : sourceVertex[indices.Offset + influence];
                            if (indexBytes == 8)
                                BinaryPrimitives.WriteUInt16LittleEndian(destinationVertex[(indices.Offset + influence * 2)..], index);
                            else
                                destinationVertex[indices.Offset + influence] = checked((byte)index);
                        }
                        sourceVertex[(indices.Offset + originalIndexBytes)..]
                            .CopyTo(destinationVertex[(indices.Offset + indexBytes)..]);
                    }
                    var declaration = new List<VertexElement>(elements.Length);
                    foreach (var element in elements)
                    {
                        var attribute = element.VertexElement;
                        if ((!hasNormals && attribute.SemanticName == "NORMAL")
                            || (!hasTangents && attribute.SemanticName == "TANGENT"))
                            continue;
                        var format = attribute.SemanticName == "BLENDINDICES"
                            ? (indexBytes == 8 ? PixelFormat.R16G16B16A16_UInt : PixelFormat.R8G8B8A8_UInt)
                            : attribute.Format;
                        int offset = element.Offset > indices.Offset ? element.Offset + indexSizeChange : element.Offset;
                        declaration.Add(new VertexElement(attribute.SemanticName, attribute.SemanticIndex, format, offset));
                    }
                    var buffer = Stride.Graphics.Buffer.New(GraphicsDevice, vertices.AsSpan(), stride, BufferFlags.VertexBuffer).DisposeBy(this);
                    mesh.Draw = new MeshDraw
                    {
                        PrimitiveType = mesh.Draw.PrimitiveType,
                        IndexBuffer = mesh.Draw.IndexBuffer,
                        StartLocation = mesh.Draw.StartLocation,
                        DrawCount = mesh.Draw.DrawCount,
                        VertexBuffers = [new VertexBufferBinding(buffer, new VertexDeclaration(declaration.ToArray(), 0, stride), binding.Count, stride, prefixBytes)],
                    };
                }
            }
            // Imported meshes leave tangent skinning disabled by default; this fixture exercises it explicitly.
            foreach (var mesh in model.Meshes)
            {
                if (mesh.Skinning == null)
                    continue;
                mesh.Parameters.Set(MaterialKeys.HasSkinningTangent, hasTangents);
                if (!hasNormals)
                    mesh.Parameters.Set(MaterialKeys.HasSkinningNormal, false);
            }
            var clip = Content.Load<AnimationClip>("MorphDonut Animation");
            var descriptor = new MaterialDescriptor();
            if (hasTangents)
            {
                descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(new ComputeShaderClassColor { MixinReference = "DeformationBasisColor" });
                descriptor.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();
            }
            else
            {
                descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(new ComputeColor(Color.White));
            }
            var material = Material.New(GraphicsDevice, descriptor);
            foreach (var instance in model.Materials)
                instance.Material = material;
            AmbientLight.Intensity = 0.25f;
            var keyLight = new Entity { new LightComponent { Type = new LightDirectional { Shadow = { Enabled = true } }, Intensity = 0.75f } };
            keyLight.Transform.Rotation = Quaternion.RotationYawPitchRoll(-0.5f, -MathUtil.PiOverFour, 0);
            Scene.Entities.Add(keyLight);
            for (int index = 0; index < DonutCount; index++)
            {
                var donut = new Entity { new ModelComponent { Model = model } };
                donut.Transform.Position = new Vector3((index - (DonutCount - 1) / 2.0f) * DonutSpacing, 0, 0);
                donut.GetOrCreate<AnimationComponent>().Animations.Add("Animation", clip);
                Scene.Entities.Add(donut);
                donuts.Add(donut);
            }

            var camera = new TestCamera(Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor);
            CameraComponent = camera.Camera;
            Script.Add(camera);
            camera.Position = new Vector3(0, 8.0f, 18.0f);
            camera.SetTarget(donuts[DonutCount / 2], true);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            if (testBatchGrowth)
            {
                var hidden = new Entity[4];
                var model = donuts[0].Get<ModelComponent>().Model;
                var firstPosition = donuts[0].Transform.Position;
                FrameGameSystem.Update(1, () =>
                {
                    donuts[0].Transform.Position = new Vector3(0, 0, 60);
                    // Five existing instances occupy eight slots. The ninth forces growth while the first is hidden.
                    for (int index = 0; index < hidden.Length; index++)
                    {
                        var extra = new Entity { new ModelComponent(model) };
                        extra.Transform.Position = new Vector3(0, 0, 60 + index * 5);
                        Scene.Entities.Add(extra);
                        hidden[index] = extra;
                    }
                });
                FrameGameSystem.Update(2, () => donuts[0].Transform.Position = firstPosition);
                FrameGameSystem.Update(3, () =>
                {
                    foreach (var extra in hidden)
                        Scene.Entities.Remove(extra);
                });
            }

            if (testLifecycle)
            {
                var component = donuts[0].Get<ModelComponent>();
                var originalModel = component.Model;
                var unskinnedModel = new Model();
                foreach (var mesh in originalModel.Meshes)
                    unskinnedModel.Meshes.Add(new Mesh(mesh.Draw, mesh.Parameters)
                    {
                        MorphTargets = mesh.MorphTargets,
                        MaterialIndex = mesh.MaterialIndex,
                    });
                foreach (var material in originalModel.Materials)
                    unskinnedModel.Materials.Add(material);

                Action[] transitions =
                [
                    () => { },
                    () => component.Morphs.Enabled = false,
                    () => component.Enabled = false,
                    () => { component.Enabled = true; component.Morphs.Enabled = true; },
                    () => component.Model = null,
                    () => component.Model = unskinnedModel,
                    () => component.Model = originalModel,
                    () => Scene.Entities.Remove(donuts[0]),
                    () => Scene.Entities.Add(donuts[0]),
                    () => component.Model = unskinnedModel,
                    () => component.Model = originalModel,
                ];
                for (int index = 0; index < transitions.Length; index++)
                {
                    FrameGameSystem.Update(index * 2, transitions[index]);
                    int checkFrame = index * 2 + 1;
                    FrameGameSystem.Draw(checkFrame, () => { });
                    lifecycleCheckFrames.Add(checkFrame);
                }
                return;
            }

            int frame = 0;

            // Bind pose: every weight is 0 and no animation plays.
            FrameGameSystem.Draw(frame, () => { }).TakeScreenshot(frame);

            // Morph weights alone, a different combination on each donut, including a negative weight.
            frame += 2;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                SetWeights(0, ("Bigger", 1.0f));
                SetWeights(1, ("Thicker", 1.0f));
                SetWeights(2, ("Wider", 0.5f), ("Flatter", 0.5f));
                SetWeights(3, ("Flatter", -0.5f));
                SetWeights(4, ("Bigger", 0.25f), ("Thicker", 0.5f), ("Wider", 0.5f));
            }).TakeScreenshot(frame);

            // Morphs and skinning: the clip animates the bones and the weights, paused at a different time on each donut.
            frame += 2;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                for (int index = 0; index < DonutCount; index++)
                {
                    var playing = donuts[index].Get<AnimationComponent>().Play("Animation");
                    playing.Enabled = false;
                    playing.CurrentTime = TimeSpan.FromSeconds(0.3 + 0.35 * index);
                }
            }).TakeScreenshot(frame);

            // Hold the animated pose for another screenshot on this lane's own baseline.
            frame += 2;
            FrameGameSystem.Draw(frame, () => { }).TakeScreenshot(frame);

            // A donut scaled to 0 has no inverse world matrix: it is skipped, the others still render.
            frame += 2;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                donuts[DonutCount / 2].Transform.Scale = Vector3.Zero;
            }).TakeScreenshot(frame);

            // A donut behind the camera (at z = 18, looking at the origin) is not collected by any view, so it is not
            // deformed. The other four, the middle one at its normal scale again, are.
            frame += 2;
            var firstDonutPosition = donuts[0].Transform.Position;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                donuts[DonutCount / 2].Transform.Scale = Vector3.One;
                donuts[0].Transform.Position = new Vector3(0, 0, 60.0f);
            });
            countFrames.Add(frame);

            // Back in view: the donut is deformed in the first frame it is visible again, not one frame later.
            frame += 1;
            FrameGameSystem.Update(frame, () => donuts[0].Transform.Position = firstDonutPosition).TakeScreenshot(frame);

            // Above the camera's view, under a light pointing down: only a shadow view collects the donut, which must
            // deform it, so its shadow on the ground is deformed too.
            frame += 2;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                AddGroundAndShadowLight();
                donuts[0].Transform.Position = new Vector3(0, 12.0f, -6.0f);
            }).TakeScreenshot(frame);
            countFrames.Add(frame);
        }

        // The shared test compositor renders no shadows; the shadow step needs directional light shadow maps.
        private void EnableDirectionalShadows()
        {
            var compositor = SceneSystem.GraphicsCompositor;
            var shadowCasterStage = compositor.RenderStages.First(stage => stage.Name == "ShadowMapCaster");
            var meshFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            var lightingFeature = meshFeature.RenderFeatures.OfType<ForwardLightingRenderFeature>().First();
            lightingFeature.ShadowMapRenderer = new ShadowMapRenderer
            {
                Renderers = { new LightDirectionalShadowMapRenderer { ShadowCasterRenderStage = shadowCasterStage } },
            };
        }

        private void AddGroundAndShadowLight()
        {
            // A neutral ground makes the deformed shadows visible.
            var material = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.DarkGray)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            });
            GeometricPrimitive<VertexPositionNormalTexture> plane = GeometricPrimitive.Plane.New(GraphicsDevice, 60.0f, 60.0f, normalDirection: NormalDirection.UpY);
            var ground = new Entity { new ModelComponent { Model = new Model { new Mesh { Draw = plane.ToMeshDraw() }, new MaterialInstance(material) } } };
            ground.Transform.Position = new Vector3(0, -1.5f, 0);
            Scene.Entities.Add(ground);

            var light = new Entity { new LightComponent { Type = new LightDirectional { Shadow = { Enabled = true } }, Intensity = 0.5f } };
            // A directional light points along its forward axis (-Z): turn it to point down (-Y).
            light.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);
            Scene.Entities.Add(light);
        }

        protected override void Draw(Stride.Games.GameTime gameTime)
        {
            base.Draw(gameTime);
            if (testBatchGrowth && FrameGameSystem.CurrentFrame <= 2)
            {
                int frame = FrameGameSystem.CurrentFrame;
                var processor = SceneSystem.SceneInstance.GetProcessor<ModelRenderProcessor>();
                if (frame > 0)
                    Assert.Equal(frame == 1 ? 5 : 4, processor.DeformationRenderer.UncollectedModelCount);
                if (ModelRenderProcessor.DeformationOverride.Mode == MeshDeformationMode.Compute)
                {
                    var output = processor.RenderModels[donuts[0].Get<ModelComponent>()].Meshes[0].ActiveMeshDraw.VertexBuffers[1].Buffer;
                    if (frame == 0)
                        outputBeforeGrowth = output;
                    else
                    {
                        Assert.NotSame(outputBeforeGrowth, output);
                        Assert.Equal(outputBeforeGrowth.SizeInBytes * 2, output.SizeInBytes);
                    }
                    outputSlots.Clear();
                    foreach (var donut in donuts)
                        AssertDeformationBindings(processor, donut.Get<ModelComponent>(), outputSlots);
                }
            }
            if (lifecycleCheckFrames.Contains(FrameGameSystem.CurrentFrame))
            {
                var processor = SceneSystem.SceneInstance.GetProcessor<ModelRenderProcessor>();
                var component = donuts[0].Get<ModelComponent>();
                Assert.Equal(donuts[0].Scene == Scene, processor.RenderModels.ContainsKey(component));
                outputSlots.Clear();
                foreach (var donut in donuts)
                {
                    if (donut.Scene == Scene)
                        AssertDeformationBindings(processor, donut.Get<ModelComponent>(), outputSlots);
                }
                lifecycleChecksRun++;
            }
            if (countFrames.Contains(FrameGameSystem.CurrentFrame))
            {
                var modelProcessor = SceneSystem.SceneInstance.GetProcessor<ModelRenderProcessor>();
                uncollectedModelCounts[FrameGameSystem.CurrentFrame] = modelProcessor.DeformationRenderer.UncollectedModelCount;
            }
        }

        private void SetWeights(int donutIndex, params (string Name, float Weight)[] weights)
        {
            var model = donuts[donutIndex].Get<ModelComponent>();
            foreach (var (name, weight) in weights)
                model.SetMorphWeight(name, weight);
        }

        private static void AssertDeformationBindings(ModelRenderProcessor processor, ModelComponent component,
            HashSet<(Stride.Graphics.Buffer Buffer, int Offset)> outputSlots)
        {
            var renderedModel = processor.RenderModels[component];
            if (component.Model == null)
            {
                foreach (var mesh in renderedModel.Meshes)
                    Assert.DoesNotContain(mesh, processor.VisibilityGroup.RenderObjects);
                return;
            }
            if (!component.Enabled)
            {
                foreach (var mesh in renderedModel.Meshes)
                    Assert.False(mesh.Enabled);
                return;
            }
            for (int meshIndex = 0; meshIndex < component.Model.Meshes.Count; meshIndex++)
            {
                var sourceMesh = component.Model.Meshes[meshIndex];
                bool morph = component.Morphs.Enabled && sourceMesh.MorphTargets != null;
                bool compute = ModelRenderProcessor.DeformationOverride.Mode == MeshDeformationMode.Compute
                    && (morph || sourceMesh.Skinning != null);
                var material = renderedModel.Materials[meshIndex];
                for (int pass = 0; pass < material.MeshCount; pass++)
                {
                    var renderedMesh = renderedModel.Meshes[material.MeshStartIndex + pass];
                    if (compute)
                    {
                        Assert.NotSame(sourceMesh, renderedMesh.Mesh);
                        Assert.Null(renderedMesh.Mesh.Skinning);
                        Assert.Equal(2, renderedMesh.ActiveMeshDraw.VertexBuffers.Length);
                        if (pass == 0)
                        {
                            var output = renderedMesh.ActiveMeshDraw.VertexBuffers[1];
                            Assert.True(outputSlots.Add((output.Buffer, output.Offset)), "Two meshes share a compute output slot.");
                        }
                    }
                    else
                    {
                        Assert.Same(sourceMesh, renderedMesh.Mesh);
                        Assert.Same(sourceMesh.Draw, renderedMesh.ActiveMeshDraw);
                    }
                    Assert.Equal(morph && !compute, renderedMesh.MorphBuffers != null);
                }
            }
        }

        // The donut behind the camera is the only one not deformed; the donut seen only by a shadow view is deformed.
        private void AssertOnlyHiddenDonutUncollected()
        {
            var counts = new List<int>();
            foreach (var countFrame in countFrames)
                counts.Add(uncollectedModelCounts.GetValueOrDefault(countFrame, -1));
            Assert.Equal(new[] { 1, 0 }, counts);
        }

        [Theory]
        [InlineData("Vertex", false)]
        [InlineData("ComputeSingle", false)]
        [InlineData("ComputeBatched", false)]
        [InlineData("Vertex", true)]
        [InlineData("ComputeSingle", true)]
        [InlineData("ComputeBatched", true)]
        public void RunTestGame(string path, bool lifecycle)
        {
            var game = new MorphTargetTests { TestName = lifecycle ? path + ".Lifecycle" : path, testLifecycle = lifecycle };
            RunPath(game, path);
        }

        [Theory]
        [InlineData("Vertex")]
        [InlineData("ComputeSingle")]
        [InlineData("ComputeBatched")]
        public void RunBatchGrowthTestGame(string path)
        {
            RunPath(new MorphTargetTests { TestName = path, testBatchGrowth = true }, path);
        }

        [Theory]
        [InlineData("Vertex", "Offset")]
        [InlineData("ComputeSingle", "Offset")]
        [InlineData("ComputeBatched", "Offset")]
        [InlineData("Vertex", "Indices8")]
        [InlineData("ComputeSingle", "Indices8")]
        [InlineData("ComputeBatched", "Indices8")]
        [InlineData("Vertex", "Indices16")]
        [InlineData("ComputeSingle", "Indices16")]
        [InlineData("ComputeBatched", "Indices16")]
        [InlineData("Vertex", "PositionOnly")]
        [InlineData("ComputeSingle", "PositionOnly")]
        [InlineData("ComputeBatched", "PositionOnly")]
        [InlineData("Vertex", "NoTangent")]
        [InlineData("ComputeSingle", "NoTangent")]
        [InlineData("ComputeBatched", "NoTangent")]
        public void RunVertexLayoutTestGame(string path, string layout)
        {
            bool lifecycle = layout is "PositionOnly" or "NoTangent";
            RunPath(new MorphTargetTests { TestName = path, testVertexLayout = layout, testLifecycle = lifecycle }, path);
        }

        private static void RunPath(MorphTargetTests game, string path)
        {
            var previousOverride = ModelRenderProcessor.DeformationOverride;
            try
            {
                ModelRenderProcessor.DeformationOverride = path switch
                {
                    "Vertex" => VertexShaderPath,
                    "ComputeSingle" => ComputeSingleInstancePath,
                    "ComputeBatched" => ComputeBatchedPath,
                    _ => throw new ArgumentOutOfRangeException(nameof(path)),
                };
                // Equivalent vertex layouts and batch growth reuse this path's existing golds.
                RunGameTest(game);
                if (game.testLifecycle)
                    Assert.Equal(game.lifecycleCheckFrames.Count, game.lifecycleChecksRun);
                else
                    game.AssertOnlyHiddenDonutUncollected();
            }
            finally
            {
                ModelRenderProcessor.DeformationOverride = previousOverride;
            }
        }
    }
}
