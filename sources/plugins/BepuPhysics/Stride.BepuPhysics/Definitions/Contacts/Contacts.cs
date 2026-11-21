// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.Contracts;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions.Contacts;

/// <summary>
/// Enumerate over this structure to get individual contact
/// </summary>
/// <code>
/// <![CDATA[
/// void OnStartedTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
/// {
///    foreach (var contact in contacts)
///    {
///        contact.Normal ...
///    }
/// }
/// ]]>
/// </code>
public readonly ref struct Contacts<TManifold> where TManifold : unmanaged, IContactManifold<TManifold>
{
    /// <summary>
    /// Contact group registered between these two collidables, one per compound child or body pair hit
    /// </summary>
    public required ReadOnlySpan<ContactGroup<TManifold>> Groups { get; init; }

    /// <summary>
    /// The simulation this contact occured in
    /// </summary>
    public required BepuSimulation Simulation { get; init; }

    /// <summary>
    /// Whether <see cref="EventSource"/> maps to the unsorted, original A
    /// </summary>
    /// <remarks> For the first of <see cref="Groups"/>; when either collidable spans multiple bodies, each <see cref="Contact{TManifold}"/> accounts for its own group </remarks>
    public required bool IsSourceOriginalA { get; init; }

    /// <summary>
    /// The collidable which is bound to this <see cref="IContactHandler"/>
    /// </summary>
    public required CollidableComponent EventSource { get; init; }

    /// <summary>
    /// The other collidable
    /// </summary>
    public required CollidableComponent Other { get; init; }

    [Pure]
    public Vector3 ComputeImpactForce(Contact<TManifold> contact)
    {
        var impactPos = contact.Point;
        var pair = contact.ContactGroup.Pair;
        ImpactOf(Other, contact.IsSourceA ? pair.B : pair.A, impactPos, out var impactVelOther, out var invMassOther);
        ImpactOf(EventSource, contact.IsSourceA ? pair.A : pair.B, impactPos, out var impactVelThis, out var invMassThis);

        var relativeImpactVel = impactVelOther - impactVelThis;
        if (invMassOther + invMassThis <= 0f)
            return default;
        float effectiveMass = 1f / (invMassOther + invMassThis);
        return relativeImpactVel * effectiveMass / (float)Simulation.FixedTimeStepSeconds;
    }

    private void ImpactOf(CollidableComponent component, CollidableReference touched, Vector3 impactPos, out Vector3 velocity, out float inverseMass)
    {
        if (component is BodyComponent body)
        {
            velocity = body.PreviousLinearVelocity + Vector3.Cross(body.PreviousAngularVelocity, impactPos - body.Position);
            inverseMass = body.BodyInertia.InverseMass;
        }
        else if (touched.Mobility != CollidableMobility.Static)
        {
            // A collidable spanning multiple bodies, only the body that touched takes part in the impact
            var reference = Simulation.Simulation.Bodies[touched.BodyHandle];
            velocity = reference.Velocity.Linear.ToStride();
            inverseMass = reference.LocalInertia.InverseMass;
        }
        else
        {
            velocity = default;
            inverseMass = 0;
        }
    }

    /// <inheritdoc cref="Contacts{TManifold}"/>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// The enumerator for <see cref="Contacts{TManifold}"/>
    /// </summary>
    /// <inheritdoc cref="Contacts{TManifold}"/>
    public ref struct Enumerator(Contacts<TManifold> data)
    {
        private int _infoIndex = 0;
        private int _manifoldIndex = -1;
        private Contacts<TManifold> _data = data;

        public bool MoveNext()
        {
            for (; _infoIndex < _data.Groups.Length; _infoIndex++)
            {
                var manifold = _data.Groups[_infoIndex].Manifold;
                while (_manifoldIndex + 1 < manifold.Count)
                {
                    _manifoldIndex += 1;
                    if (manifold.GetDepth(_manifoldIndex) >= 0)
                        return true;
                }

                _manifoldIndex = -1;
            }

            return false;
        }

        public Contact<TManifold> Current => new(_manifoldIndex, _data, in _data.Groups[_infoIndex]);
    }
}
