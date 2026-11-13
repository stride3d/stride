// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// What the particles of a <see cref="SoftBodyPin"/> are tied to: a <see cref="BodyAnchor"/> or a <see cref="WorldAnchor"/>.
/// </summary>
[DataContract(Inherited = true)]
public abstract class SoftBodyAnchor
{
    private protected SoftBodyAnchor()
    {
    }

    /// <summary>
    /// The name of a node of the model on the anchor's entity, such as a bone, used instead of the entity itself.
    /// </summary>
    /// <remarks>
    /// Null or empty uses the entity; a name the model does not have is reported and the entity is used instead.
    /// A <see cref="WorldAnchor"/> follows the node. For a <see cref="BodyAnchor"/> it only says where a pin without a region takes its particles.
    /// </remarks>
    [DataMember(100)]
    public string? NodeName { get; set; }

    /// <summary> The entity whose transform, or whose node, the pinned particles follow </summary>
    internal abstract Entity? AnchorEntity { get; }
}

/// <summary>
/// Joins the pinned particles to the bodies of a collidable: they pull on each other, like a rope tied to a crate.
/// </summary>
/// <remarks>
/// Each particle is joined to the nearest body of <see cref="Body"/>; to a soft body, it is joined to its nearest particles.
/// Joined to a <see cref="StaticComponent"/>, the particles are held as by a <see cref="WorldAnchor"/> on its entity.
/// A pin holding many particles joins a few dozen of them, spread over the pin, and the others follow through the soft body:
/// every joint is one more constraint for the solver to fit on the body.
/// The particles are free for as long as <see cref="Body"/> is not part of the simulation of the soft body.
/// </remarks>
[DataContract]
[Display("Body")]
public sealed class BodyAnchor : SoftBodyAnchor
{
    /// <summary>
    /// The collidable the pinned particles are joined to: a body, a soft body, or any collidable made of several bodies.
    /// </summary>
    [DataMember(10)]
    public CollidableComponent? Body { get; set; }

    internal override Entity? AnchorEntity => Body?.Entity;
}

/// <summary>
/// Holds the pinned particles at points of the world, where they were created or carried by an entity, without acting on anything.
/// </summary>
/// <remarks> The particles still give way to a pull stronger than <see cref="SoftBodyPin.ServoMaximumForce"/>, and come back. </remarks>
[DataContract]
[Display("World")]
public sealed class WorldAnchor : SoftBodyAnchor
{
    /// <summary>
    /// The entity carrying the points, which keep the offset they had from it when the soft body was created; null keeps them in place.
    /// </summary>
    [DataMember(10)]
    public Entity? Entity { get; set; }

    internal override Entity? AnchorEntity => Entity;
}
