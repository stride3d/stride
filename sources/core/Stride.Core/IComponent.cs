// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core;

/// <summary>
///   Base interface for all framework Components.
/// </summary>
/// <remarks>
///   A <strong>Component</strong> is an object that can have an optional <see cref="Name"/>, and that
///   has reference-counting lifetime management.
/// </remarks>
/// <seealso cref="IReferencable"/>
public interface IComponent : IReferencable
{
    /// <summary>
    ///   Gets the name of the Component.
    /// </summary>
    string Name { get; }
}
