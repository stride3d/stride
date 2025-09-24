// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.CollisionDetection;

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
    public ReadOnlySpan<ContactGroup<TManifold>> Groups { get; }

    /// <summary>
    /// The simulation this contact occured in
    /// </summary>
    public BepuSimulation Simulation { get; }

    /// <summary>
    /// Whether <see cref="EventSource"/> maps to the unsorted, original A
    /// </summary>
    public bool IsSourceOriginalA { get; }

    /// <summary>
    /// The collidable which is bound to this <see cref="IContactHandler"/>
    /// </summary>
    public CollidableComponent EventSource { get; }

    /// <summary>
    /// The other collidable
    /// </summary>
    public CollidableComponent Other { get; }

    public Contacts(CollidableComponent source, CollidableComponent other, bool isSourceOriginalA, ReadOnlySpan<ContactGroup<TManifold>> groups, BepuSimulation simulation)
    {
        EventSource = source;
        Other = other;
        IsSourceOriginalA = isSourceOriginalA;
        Groups = groups;
        Simulation = simulation;
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
