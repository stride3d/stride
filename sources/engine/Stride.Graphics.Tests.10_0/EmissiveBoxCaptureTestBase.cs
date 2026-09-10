// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;

namespace Stride.Graphics.Tests;

/// <summary>
/// A game that captures something from a scene made of emissive quads, before its loop starts drawing.
/// </summary>
/// <remarks>
/// <para>
///   Emissive is unlit, so a quad's colour does not fall off with distance or angle: every direction that
///   ends on a quad has exactly that quad's colour, wherever the capture point is. That makes what a
///   capture has to contain follow from the geometry alone.
/// </para>
/// <para>
///   The capture runs from <see cref="LoadContent"/> on purpose. It renders the scene again through the
///   compositor's <see cref="GraphicsCompositor.SingleView"/>, and doing that while a frame is in flight
///   hands back temporary render targets the frame still holds.
/// </para>
/// </remarks>
public abstract class EmissiveBoxCaptureTestBase : GameTestBase
{
    protected Scene Scene { get; private set; }

    protected EmissiveBoxCaptureTestBase()
    {
        // Captures copy rendered faces into a texture cube
        GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_10_0];
        GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_10_0;
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        SceneSystem.GraphicsCompositor = Content.Load<GraphicsCompositor>("GraphicsCompositor");

        Scene = new Scene();
        Scene.Entities.Add(new Entity("Camera") { new CameraComponent { Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId() } });
        PopulateScene();
        SceneSystem.SceneInstance = new SceneInstance(Services, Scene);

        Capture();
    }

    /// <summary>
    /// Adds the entities to capture to <see cref="Scene"/>.
    /// </summary>
    protected abstract void PopulateScene();

    /// <summary>
    /// Captures from the populated scene, before the game loop draws anything.
    /// </summary>
    protected abstract void Capture();

    /// <summary>
    /// Adds a square emissive quad perpendicular to an axis. Culling is off, so it is visible from both
    /// sides whichever way round it ends up.
    /// </summary>
    protected Entity AddEmissiveQuad(Vector3 axis, Vector3 position, float size, Color3 color)
    {
        var material = Material.New(GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.Black)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Emissive = new MaterialEmissiveMapFeature(
                    new ComputeColor(new Color4(color.R, color.G, color.B, 1.0f))),
                CullMode = CullMode.None,
            },
        });

        var model = new ProceduralModelDescriptor(
            new PlaneProceduralModel
            {
                Size = new Vector2(size),
                MaterialInstance = { Material = material },
            }).GenerateModel(Services);

        var entity = new Entity($"Quad{axis}") { new ModelComponent { Model = model } };
        entity.Transform.Position = position;

        // The plane is authored in the XZ plane, so turn it to face along the axis
        if (axis.X != 0.0f)
            entity.Transform.Rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);
        else if (axis.Z != 0.0f)
            entity.Transform.Rotation = Quaternion.RotationX(MathUtil.PiOverTwo);

        Scene.Entities.Add(entity);

        return entity;
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();

        // Nothing to render: the capture happened in LoadContent. This only gives the game a frame to run
        // so that it starts up and exits
        FrameGameSystem.Update(() => { });
    }
}
