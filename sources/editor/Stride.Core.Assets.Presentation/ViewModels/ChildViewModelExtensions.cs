// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

/// <summary>
/// Extension methods for <see cref="IChildViewModel"/>.
/// </summary>
public static class ChildViewModelExtensions
{
    /// <summary>
    /// Checks whether the given collection is comprised of children of the same parent.
    /// </summary>
    /// <param name="collection">The children.</param>
    /// <returns><c>true</c> if the collection is not empty and all children have the same parent; otherwise, <c>false</c>.</returns>
    public static bool AllSiblings(this IEnumerable<IChildViewModel> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var parents = collection.Select(x => x.GetParent()).ToList();
        return parents.Count > 0 && parents.All(x => x == parents[0]);
    }
}
