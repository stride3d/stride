// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// A collection of contributions of a plugin, removed when the plugin leaves.
/// </summary>
/// <typeparam name="T">The type of the contributions.</typeparam>
public interface IPluginRegistry<in T>
    where T : notnull
{
    /// <summary>
    /// Adds a contribution in the name of the plugin.
    /// </summary>
    void Register(T item);
}
