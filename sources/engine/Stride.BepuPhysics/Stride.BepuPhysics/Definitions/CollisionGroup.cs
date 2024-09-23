// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;

namespace Stride.BepuPhysics.Definitions;

/// <summary>
/// This value follows up <see cref="CollisionLayer"/> when filtering out collisions.
/// It prevents collisions between objects sharing the same <see cref="CollisionGroup.Id"/>,
/// when the absolute difference between their <see cref="CollisionGroup.IndexA"/>,
/// <see cref="CollisionGroup.IndexB"/>, and <see cref="CollisionGroup.IndexC"/> is less than two.
/// </summary>
/// <remarks>
/// Example A: You have multiple characters (A, B, C, D) all with the same <see cref="CollisionLayer"/>, they are split in two teams {A, B} and {C, D},
/// but you don't want members of the same team to collide between each other, you can set {A, B}'s <see cref="Id"/> to 1 and {C, D}'s <see cref="Id"/> to 2.
/// <para/>
/// Example B: You have a chain of three colliders attached to each other {A, B, C}, you don't want A and C to collide with B, but A and C should collide together.
/// Set A, B and C's Ids to 1 to start filtering, leave A's <see cref="CollisionGroup.IndexA"/> at 0, B's to 1 and C to 2.
/// A and C will collide since the difference between their <see cref="CollisionGroup.IndexA"/> is equal to two,
/// but neither of them will collide with B since they are both only one away from B's index.
/// </remarks>
[DataContract]
public struct CollisionGroup
{
    /// <summary>
    /// The identification number used by this object, zero when you don't want to filter more than <see cref="CollisionLayer"/> already does.
    /// </summary>
    /// <remarks>
    /// Set this value to the same number for all objects which should have their collisions ignored when colliding between each other.
    /// </remarks>
    [DefaultValue(0)]
    public ushort Id;
    /// <summary>
    /// An index associated with this object in the collision group <see cref="Id"/>
    /// </summary>
    /// <remarks>
    /// For a bunch of objects sharing the same <see cref="Id"/>,
    /// if the two objects have the same or subsequent values, collision between those two objects will be ignored.
    /// But, if their values are more than one apart, they will collide with each other.
    /// </remarks>
    [DataAlias("XAxis"), DefaultValue(0)]
    public ushort IndexA;
    /// <inheritdoc cref="IndexA"/>
    [DataAlias("YAxis"), DefaultValue(0)]
    public ushort IndexB;
    /// <inheritdoc cref="IndexA"/>
    [DataAlias("ZAxis"), DefaultValue(0)]
    public ushort IndexC;
}
