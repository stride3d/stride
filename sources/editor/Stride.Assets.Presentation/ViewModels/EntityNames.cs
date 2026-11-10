// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// The names of new entities in an entity hierarchy.
/// </summary>
public static class EntityNames
{
    /// <summary>
    /// Gets the name of a new entity under <paramref name="parent"/>.
    /// </summary>
    /// <param name="parent">The parent of the new entity.</param>
    /// <param name="baseName">The name the new entity would have on its own.</param>
    /// <returns>The name of the new entity.</returns>
    /// <remarks>Entity names are not made unique among their siblings any more, so this returns <paramref name="baseName"/>.</remarks>
    public static string ComputeNewName(IEntityHierarchyItemViewModel parent, string baseName)
    {
        // We do not generate unique names for entities anymore, it is not useful at all.
        //return NamingHelper.ComputeNewName(baseName, parent.Children.OfType<EntityViewModel>(), x => x.Name);
        return baseName;
    }
}
