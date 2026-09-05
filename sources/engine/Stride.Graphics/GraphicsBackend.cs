// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   What the graphics backend compiled into this build has implemented.
/// </summary>
/// <remarks>
///   This is a fact about Stride, not about any device, so it needs no <see cref="GraphicsDevice"/> and
///   the asset pipeline can read it. A game's platform head project sets the target graphics API, so a
///   build carries exactly one backend and answers only for that one.
/// </remarks>
public static partial class GraphicsBackend
{
    /// <summary>
    ///   Whether this backend has implemented a capability at all.
    /// </summary>
    /// <param name="kind">The capability kind to ask about.</param>
    /// <remarks>
    ///   A device that provides a capability is of no use when the backend cannot drive it. Read
    ///   <see cref="GraphicsDeviceFeatures.Supports"/> to ask both questions at once.
    /// </remarks>
    public static partial bool Implements(GraphicsCapabilityKind kind);
}
