// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract(Inherited = true)]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
[AllowMultipleComponents]
public abstract class ConstraintComponentBase : EntityComponent
{
    protected static Logger Logger = GlobalLogger.GetLogger(nameof(ConstraintComponentBase));

    private bool _enabled = true;
    private readonly BodyComponent?[] _bodies;

    public bool Enabled
    {
        get
        {
            return _enabled;
        }
        set
        {
            _enabled = value;
            TryReattachConstraint();
        }
    }

    public ReadOnlySpan<BodyComponent?> Bodies => _bodies;

    protected ConstraintComponentBase(int bodies) => _bodies = new BodyComponent?[bodies];

    protected BodyComponent? this[int i]
    {
        get => _bodies[i];
        set
        {
            _bodies[i] = value;
            BodiesChanged();
        }
    }

    /// <summary>
    /// Whether this constraint is in a valid state and actively constraining its targets.
    /// </summary>
    /// <remarks> May not be attached if it is not in a scene, when not <see cref="Enabled"/>, when any of its target is null, not in a scene or in a different simulation </remarks>
    public abstract bool Attached { get; }

    protected abstract void BodiesChanged();

    internal abstract void Activate(BepuConfiguration bepuConfig);

    internal abstract void Deactivate();

    internal abstract ConstraintState TryReattachConstraint();

    internal abstract void DetachConstraint();

    public enum ConstraintState
    {
        ConstraintNotInScene,
        ConstraintDisabled,
        BodyNotInScene,
        BodyNull,
        SimulationMismatch,
        FullyOperational,
    }
}
