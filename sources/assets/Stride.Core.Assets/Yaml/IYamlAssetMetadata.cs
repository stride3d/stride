// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;

namespace Stride.Core.Assets.Yaml;

/// <summary>
/// An interface representing a container used to transfer metadata between the asset and the YAML serializer.
/// </summary>
internal interface IYamlAssetMetadata : IEnumerable
{
    /// <summary>
    /// Notifies that this metadata has been attached and cannot be modified anymore.
    /// </summary>
    void Attach();

    /// <summary>
    /// Attaches the given metadata value to the given YAML path.
    /// </summary>
    /// <param name="path">The path at which to attach metadata.</param>
    /// <param name="value">The metadata to attach.</param>
    void Set(YamlAssetPath path, object value);

    /// <summary>
    /// Removes attached metadata from the given YAML path.
    /// </summary>
    /// <param name="path">The path at which to remove metadata.</param>
    void Remove(YamlAssetPath path);

    /// <summary>
    /// Tries to retrieve the metadata for the given path.
    /// </summary>
    /// <param name="path">The path at which to retrieve metadata.</param>
    /// <returns>The metadata attached to the given path, or the default value of the underlying type if no metadata is attached at the given path.</returns>
    object? TryGet(YamlAssetPath path);
}
