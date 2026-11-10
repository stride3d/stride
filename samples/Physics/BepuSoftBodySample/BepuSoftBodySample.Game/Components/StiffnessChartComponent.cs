// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Soft;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;

namespace BepuSoftBodySample.Game.Components;

/// <summary>
/// A chart made of soft cubes: their stiffness grows from left to right, another of their settings changes from front to back,
/// and the same crate lands on each of them. C picks the setting of the second axis.
/// </summary>
[ComponentCategory("BepuSoftBodyDemo")]
public class StiffnessChartComponent : SyncScript
{
    // In hertz, as the property of the bodies is
    private static readonly float[] Stiffnesses = [3f, 6f, 12f, 25f, 50f];

    // A spring much faster than this is more than the solver follows at the substeps of the sample, and the body flies apart
    private const float StiffestSpring = 200f;

    /// <summary> What changes from front to back: its name, its three values, and how a value is given to a cube and its crate </summary>
    private sealed record Axis(string Name, float[] Values, Action<VolumetricSoftBodyComponent, BoxCollider, float> Apply);

    private static readonly Axis[] Axes =
    [
        new("shear stiffness, times the stiffness (200 Hz at most)", [1f, 4f, 16f], (body, _, value) => body.ShearStiffness = MathF.Min(body.Stiffness * value, StiffestSpring)),
        new("volume stiffness, times the stiffness (200 Hz at most)", [1f, 4f, 16f], (body, _, value) => body.VolumeStiffness = MathF.Min(body.Stiffness * value, StiffestSpring)),
        new("minimum compression of the cells", [0.4f, 0.6f, 0.8f], (body, _, value) => body.MinimumCompression = value),
        new("damping ratio", [0.3f, 1f, 3f], (body, _, value) => body.Damping = value),
        new("cells along a side", [5f, 7f, 10f], (body, _, value) => body.Resolution = (int)value),
        new("mass of the crate, in kg", [6f, 2f, 0.5f], (_, crate, value) => crate.Mass = value),
    ];

    private readonly List<Entity> _entities = [];
    private readonly List<(Vector3 Position, string Text)> _labels = [];
    private int _axis;

    /// <summary> The model of the soft cubes, one meter wide </summary>
    public Model? BodyModel { get; set; }

    /// <summary> The model of the crates, a cube of one meter </summary>
    public Model? CrateModel { get; set; }

    public Material? CrateMaterial { get; set; }

    /// <summary> One per column, from the softest to the stiffest </summary>
    public List<Material> Materials { get; } = [];

    /// <summary> The cells along the longest side of each cube, unless the second axis is about them </summary>
    public int Resolution { get; set; } = 7;

    /// <summary> Between two cubes, in meters </summary>
    public float Spacing { get; set; } = 1.9f;

    /// <summary> In kilograms, unless the second axis is about it </summary>
    public float CrateMass { get; set; } = 6f;

    public override void Start()
    {
        Build();
    }

    public override void Update()
    {
        if (Input.IsKeyPressed(Keys.C))
        {
            _axis = (_axis + 1) % Axes.Length;
            Build();
        }

        var bounds = Game.Window.ClientBounds;
        var title = $"Left to right: stiffness.  Front to back: {Axes[_axis].Name}.  C: next setting ({_axis + 1}/{Axes.Length})";
        DebugText.Print(title, new Int2(bounds.Width / 2 - title.Length * 4, 12), Color.Yellow);

        if (SceneSystem.GraphicsCompositor.Cameras.Count == 0 || SceneSystem.GraphicsCompositor.Cameras[0].Camera is not { } camera)
            return;

        // The axes of the chart, written on screen where they are on the floor
        foreach (var (position, text) in _labels)
        {
            var clip = Vector3.Transform(position, camera.ViewProjectionMatrix);
            if (clip.W <= 0f)
                continue;
            var screen = new Int2((int)((clip.X / clip.W + 1f) * 0.5f * bounds.Width), (int)((1f - clip.Y / clip.W) * 0.5f * bounds.Height));
            DebugText.Print(text, screen - new Int2(text.Length * 4, 8), Color.White);
        }
    }

    public override void Cancel()
    {
        Clear();
    }

    private void Clear()
    {
        foreach (var entity in _entities)
            entity.Scene?.Entities.Remove(entity);
        _entities.Clear();
        _labels.Clear();
    }

    private void Build()
    {
        Clear();
        if (BodyModel is null || CrateModel is null)
            return;

        var axis = Axes[_axis];
        float left = -(Stiffnesses.Length - 1) * 0.5f * Spacing;
        float front = (axis.Values.Length - 1) * 0.5f * Spacing;
        for (int row = 0; row < axis.Values.Length; row++)
        {
            float z = front - row * Spacing;
            for (int column = 0; column < Stiffnesses.Length; column++)
            {
                float x = left + column * Spacing;
                var size = new Vector3(0.8f, 0.3f, 0.8f);
                var body = new VolumetricSoftBodyComponent
                {
                    Mass = 1f,
                    Resolution = Resolution,
                    Stiffness = Stiffnesses[column],
                    ShearStiffness = Stiffnesses[column],
                    VolumeStiffness = Stiffnesses[column],
                };
                var box = new BoxCollider { Mass = CrateMass, Size = size };
                axis.Apply(body, box, axis.Values[row]);

                var cube = new Entity($"Cube {Stiffnesses[column]} Hz, {axis.Name} {axis.Values[row]}") { new ModelComponent(BodyModel), body };
                if (Materials.Count > 0)
                    cube.Get<ModelComponent>()!.Materials[0] = Materials[column % Materials.Count];
                cube.Transform.Position = new Vector3(x, 0.62f, z);
                cube.Transform.Scale = new Vector3(1f, 1.2f, 1f);

                var crate = new Entity("Crate")
                {
                    new ModelComponent(CrateModel),
                    new BodyComponent { Collider = new CompoundCollider { Colliders = { box } } },
                    new LaunchComponent { Delay = 0.6f, HoldUntilLaunch = true },
                };
                if (CrateMaterial is not null)
                    crate.Get<ModelComponent>()!.Materials[0] = CrateMaterial;
                crate.Transform.Position = new Vector3(x, 2.6f, z);
                crate.Transform.Scale = size;

                _entities.Add(cube);
                _entities.Add(crate);
                Entity.Scene.Entities.Add(cube);
                Entity.Scene.Entities.Add(crate);
            }
        }

        for (int column = 0; column < Stiffnesses.Length; column++)
            _labels.Add((new Vector3(left + column * Spacing, 0f, front + Spacing * 0.65f), $"{Stiffnesses[column]} Hz"));
        for (int row = 0; row < axis.Values.Length; row++)
            _labels.Add((new Vector3(left - Spacing * 0.75f, 0f, front - row * Spacing), FormattableString.Invariant($"{axis.Values[row]}")));
    }
}
