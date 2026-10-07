// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using BepuPhysics;
using BepuPhysics.Constraints;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// A soft body made from the surface of a model, like a flag, a curtain or a sheet.
/// </summary>
/// <remarks> Every vertex of the model is a particle, vertices sharing a position are merged </remarks>
[DataContract]
[Display("Cloth")]
public sealed class ClothComponent : SoftBodyComponent
{
    private float _stretchStiffness = 60f;
    private float _bendStiffness = 4f;
    private float _damping = 1f;
    private float _compression = 0.5f;
    private int[] _triangles = [];
    private float _time;

    /// <summary>
    /// How fast the edges of the triangles return to their rest length, in hertz.
    /// </summary>
    /// <remarks> Values above half the rate of the solver's substeps cannot be honored </remarks>
    [Display(category: CategorySoftBody)]
    [DefaultValue(60f)]
    public float StretchStiffness
    {
        get => _stretchStiffness;
        set
        {
            _stretchStiffness = value;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// How fast folds between adjacent triangles flatten back, in hertz, low values make a limp cloth.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [DefaultValue(4f)]
    public float BendStiffness
    {
        get => _bendStiffness;
        set
        {
            _bendStiffness = value;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// How much the edges of the triangles can shrink without resistance, as a fraction of their rest length.
    /// </summary>
    /// <remarks> Lets the cloth buckle into folds, zero makes the edges resist compression as much as stretching </remarks>
    [Display(category: CategorySoftBody)]
    [DataMemberRange(0, 0.9, 0.05, 0.1, 2)]
    [DefaultValue(0.5f)]
    public float Compression
    {
        get => _compression;
        set
        {
            _compression = Math.Clamp(value, 0f, 0.9f);
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// The damping ratio of every spring, one stops oscillations the fastest without overshooting.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [DefaultValue(1f)]
    public float Damping
    {
        get => _damping;
        set
        {
            _damping = value;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// The velocity of the air around the cloth, in world space.
    /// </summary>
    [Display(category: CategorySoftBody)]
    public Vector3 Wind { get; set; }

    /// <summary>
    /// How strongly the air pushes on the cloth when it moves relative to the wind, zero ignores the air.
    /// </summary>
    /// <remarks> Around 0.6 is physical: half the air density times the drag coefficient of a sheet </remarks>
    [Display(category: CategorySoftBody)]
    [DefaultValue(0.1f)]
    public float AirDrag { get; set; } = 0.1f;

    /// <summary>
    /// How much the wind varies over the surface and over time, as a fraction of its speed; makes flags ripple instead of flying flat.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [DataMemberRange(0, 1, 0.05, 0.1, 2)]
    [DefaultValue(0.3f)]
    public float Turbulence { get; set; } = 0.3f;

    /// <summary> Creates a cloth whose particles are spheres sized from the spacing between vertices </summary>
    [SetsRequiredMembers]
    public ClothComponent()
    {
    }

    internal override float DefaultRadiusFraction => 0.5f;

    internal override SoftBodyTopology BuildTopology(SourceMesh mesh)
    {
        var topology = ClothBuilder.Build(mesh);

        // Triangles between particles, without the duplicates a double sided model has
        var seen = new HashSet<(int, int, int)>();
        var triangles = new List<int>();
        var indices = mesh.Indices;
        for (int t = 0; t + 2 < indices.Length; t += 3)
        {
            int a = topology.BindingParticles[indices[t]], b = topology.BindingParticles[indices[t + 1]], c = topology.BindingParticles[indices[t + 2]];
            if (a == b || b == c || a == c)
                continue;
            Span<int> sorted = [a, b, c];
            sorted.Sort();
            if (seen.Add((sorted[0], sorted[1], sorted[2])))
                triangles.AddRange([a, b, c]);
        }
        _triangles = triangles.ToArray();
        return topology;
    }

    internal override void AddConstraints(global::BepuPhysics.Simulation simulation, ReadOnlySpan<BodyHandle> particles, ReadOnlySpan<bool> pinned, SoftBodyTopology topology)
    {
        var stretch = Spring(_stretchStiffness, _damping);
        var bend = Spring(_bendStiffness, _damping);
        foreach (var edge in topology.Edges)
        {
            if (pinned[edge.A] && pinned[edge.B])
                continue;
            var length = System.Numerics.Vector3.Distance(simulation.Bodies[particles[edge.A]].Pose.Position, simulation.Bodies[particles[edge.B]].Pose.Position);
            if (edge.Kind == EdgeKind.Bend)
                simulation.Solver.Add(particles[edge.A], particles[edge.B], new CenterDistanceConstraint(length, bend));
            else if (_compression > 0f)
                simulation.Solver.Add(particles[edge.A], particles[edge.B], new CenterDistanceLimit(length * (1f - _compression), length, stretch));
            else
                simulation.Solver.Add(particles[edge.A], particles[edge.B], new CenterDistanceConstraint(length, stretch));
        }
    }

    internal override void BeforeStep(float deltaTime)
    {
        if (AirDrag <= 0f || ParticleCount == 0)
            return;
        if (Wind != Vector3.Zero && Awake == false)
            Awake = true;

        _time += deltaTime;
        var gust = Wind.Length() * Turbulence;

        // Each triangle feels a pressure along its normal proportional to its area and to the square of the air speed through it
        for (int t = 0; t < _triangles.Length; t += 3)
        {
            var a = Particle(_triangles[t]);
            var b = Particle(_triangles[t + 1]);
            var c = Particle(_triangles[t + 2]);
            var pa = a.Pose.Position.ToStride();
            var pb = b.Pose.Position.ToStride();
            var pc = c.Pose.Position.ToStride();
            var wind = gust > 0f ? Wind + gust * Swirl((pa + pb + pc) / 3f, _time) : Wind;
            var doubleAreaNormal = Vector3.Cross(pb - pa, pc - pa);
            var doubleArea = doubleAreaNormal.Length();
            if (doubleArea < 1e-12f)
                continue;

            var normal = doubleAreaNormal / doubleArea;
            var relative = (a.Velocity.Linear + b.Velocity.Linear + c.Velocity.Linear).ToStride() / 3f - wind;
            var normalSpeed = Vector3.Dot(relative, normal);
            var force = normal * (-AirDrag * 0.5f * doubleArea * normalSpeed * MathF.Abs(normalSpeed));
            var impulse = (force * (deltaTime / 3f)).ToNumeric();

            Push(a, impulse);
            Push(b, impulse);
            Push(c, impulse);
        }

        // Smooth pseudo random field in [-1, 1] drifting with time, a cheap stand-in for turbulent air; the frequencies are arbitrary, picked not to line up
        static Vector3 Swirl(Vector3 p, float time) => new(
            MathF.Sin(p.Y * 2.1f + time * 4.3f) * MathF.Cos(p.Z * 1.7f - time * 2.9f),
            MathF.Sin(p.Z * 2.3f + time * 3.7f) * MathF.Cos(p.X * 1.9f + time * 3.1f),
            MathF.Sin(p.X * 2.7f - time * 5.3f) * MathF.Cos(p.Y * 1.3f + time * 2.3f));

        static void Push(BodyReference body, System.Numerics.Vector3 impulse)
        {
            if (body.Kinematic == false)
                body.Velocity.Linear += impulse * body.LocalInertia.InverseMass;
        }
    }
}
