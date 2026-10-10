// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using BepuPhysics;
using BepuPhysics.Constraints;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// A soft body filling the volume of a closed model, like jelly or rubber.
/// </summary>
/// <remarks>
/// The inside of the model is split in a lattice of cubic cells, particles sit on their corners and every vertex of the model follows the cell it is in.
/// </remarks>
[DataContract]
[Display("Volumetric Soft Body")]
public sealed class VolumetricSoftBodyComponent : SoftBodyComponent
{
    private int _resolution = 6;
    private float _stiffness = 20f;
    private float _shearStiffness = 20f;
    private float _volumeStiffness = 20f;
    private float _damping = 1f;
    private float _maximumStretch = 1.5f;
    private float _minimumCompression = 0.4f;

    /// <summary> Creates a soft body whose particles are spheres sized from the lattice </summary>
    [SetsRequiredMembers]
    public VolumetricSoftBodyComponent()
    {
    }

    /// <summary>
    /// The amount of cells along the longest side of the model, the particle count grows with its cube.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [DataMemberRange(1, 64, 1, 4, 0)]
    [DefaultValue(6)]
    public int Resolution
    {
        get => _resolution;
        set
        {
            _resolution = Math.Clamp(value, 1, 64);
            InvalidateTopology();
        }
    }

    /// <summary>
    /// How fast the edges of the cells return to their rest length, in hertz.
    /// </summary>
    /// <remarks> Values above half the rate of the solver's substeps cannot be honored </remarks>
    [Display(category: CategorySoftBody)]
    [DefaultValue(20f)]
    public float Stiffness
    {
        get => _stiffness;
        set
        {
            _stiffness = value;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// How fast the diagonals of the cells return to their rest length, in hertz, lower values let the body shear and twist more easily.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [DefaultValue(20f)]
    public float ShearStiffness
    {
        get => _shearStiffness;
        set
        {
            _shearStiffness = value;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// How fast squashed or stretched cells recover their volume, in hertz, zero lets the volume change freely.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [DefaultValue(20f)]
    public float VolumeStiffness
    {
        get => _volumeStiffness;
        set
        {
            _volumeStiffness = MathF.Max(0f, value);
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
    /// How long the edges of the cells may get relative to their rest length, whatever pulls on them.
    /// </summary>
    /// <remarks>
    /// The springs alone give in to any force strong enough, this keeps the cells from being torn apart; zero removes the limit.
    /// </remarks>
    [Display(category: CategorySoftBody)]
    [DefaultValue(1.5f)]
    public float MaximumStretch
    {
        get => _maximumStretch;
        set
        {
            _maximumStretch = value <= 0f ? 0f : MathF.Max(1f, value);
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// How short the edges of the cells may get relative to their rest length, which keeps cells from collapsing and turning inside out.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [DataMemberRange(0, 1, 0.05, 0.1, 2)]
    [DefaultValue(0.4f)]
    public float MinimumCompression
    {
        get => _minimumCompression;
        set
        {
            _minimumCompression = Math.Clamp(value, 0f, 1f);
            TryUpdateFeatures();
        }
    }

    // Over half the diagonal of a cell face, so no ray or small object slips between four neighboring particles
    internal override float DefaultRadiusFraction => 0.72f;

    internal override SoftBodyTopology BuildTopology(SourceMesh mesh) => LatticeBuilder.Build(mesh, _resolution, DefaultRadiusFraction);

    internal override void AddConstraints(global::BepuPhysics.Simulation simulation, ReadOnlySpan<BodyHandle> particles, ReadOnlySpan<bool> pinned, SoftBodyTopology topology)
    {
        var stretch = Spring(_stiffness, _damping);
        var shear = Spring(_shearStiffness, _damping);
        var limit = Spring(MathF.Max(60f, _stiffness * 3f), 1f);
        foreach (var edge in topology.Edges)
        {
            if (pinned[edge.A] && pinned[edge.B])
                continue;
            var length = System.Numerics.Vector3.Distance(Position(simulation, particles, edge.A), Position(simulation, particles, edge.B));
            simulation.Solver.Add(particles[edge.A], particles[edge.B], new CenterDistanceConstraint(length, edge.Kind == EdgeKind.Stretch ? stretch : shear));

            // The edges along the lattice axes are enough to bound the shape of every cell
            if (edge.Kind == EdgeKind.Stretch && (_maximumStretch > 0f || _minimumCompression > 0f))
            {
                var maximum = _maximumStretch > 0f ? length * _maximumStretch : float.MaxValue;
                simulation.Solver.Add(particles[edge.A], particles[edge.B], new CenterDistanceLimit(length * _minimumCompression, maximum, limit));
            }
        }

        if (_volumeStiffness <= 0f)
            return;

        var volume = Spring(_volumeStiffness, _damping);
        foreach (var t in topology.Tetrahedra)
        {
            if (pinned[t.A] && pinned[t.B] && pinned[t.C] && pinned[t.D])
                continue;
            simulation.Solver.Add(particles[t.A], particles[t.B], particles[t.C], particles[t.D], new VolumeConstraint(Position(simulation, particles, t.A), Position(simulation, particles, t.B), Position(simulation, particles, t.C), Position(simulation, particles, t.D), volume));
        }
    }

    private static System.Numerics.Vector3 Position(global::BepuPhysics.Simulation simulation, ReadOnlySpan<BodyHandle> particles, int particle) => simulation.Bodies[particles[particle]].Pose.Position;
}
