// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.Contracts;
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
    /// Contact group registered between these two bodies, one per compound child hit
    /// </summary>
    public required ReadOnlySpan<ContactGroup<TManifold>> Groups { get; init; }

    /// <summary>
    /// The simulation this contact occured in
    /// </summary>
    public required BepuSimulation Simulation { get; init; }

    /// <summary>
    /// Whether <see cref="EventSource"/> maps to the unsorted, original A
    /// </summary>
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
        float invMassOther, invMassThis;
        Vector3 impactVelOther, impactVelThis;
        if (Other is BodyComponent bodyOther)
        {
            impactVelOther = bodyOther.PreviousLinearVelocity + Vector3.Cross(bodyOther.PreviousAngularVelocity, impactPos - bodyOther.Position);
            invMassOther = bodyOther.BodyInertia.InverseMass;
        }
        else
        {
            impactVelOther = default;
            invMassOther = 0;
        }

        if (EventSource is BodyComponent bodySource)
        {
            impactVelThis = bodySource.PreviousLinearVelocity + Vector3.Cross(bodySource.PreviousAngularVelocity, impactPos - bodySource.Position);
            invMassThis = bodySource.BodyInertia.InverseMass;
        }
        else
        {
            impactVelThis = default;
            invMassThis = 0;
        }

        var relativeImpactVel = impactVelOther - impactVelThis;
        float effectiveMass = 1f / (invMassOther + invMassThis);
        return relativeImpactVel * effectiveMass / (float)Simulation.FixedTimeStepSeconds;
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
