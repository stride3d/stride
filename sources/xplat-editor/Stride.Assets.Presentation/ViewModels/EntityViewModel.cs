// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;

namespace Stride.Assets.Presentation.ViewModels;

public sealed class EntityViewModel : EntityHierarchyItemViewModel
{
    private string? name;

    public EntityViewModel(EntityHierarchyViewModel asset, EntityDesign entityDesign)
        : base(asset, GetOrCreateChildPartDesigns((EntityHierarchyAssetBase)asset.Asset, entityDesign))
    {
        EntityDesign = entityDesign;
    }

    public override string? Name
    {
        get => name;
        set => SetValue(ref name, value);
    }

    internal EntityDesign EntityDesign { get; }
    
    // TODO: turn this non-static and put it in the base - just keep the entity-specific part here. This need to rework a bit how we initialize folders
    private static IEnumerable<EntityDesign> GetOrCreateChildPartDesigns(EntityHierarchyAssetBase asset, EntityDesign entityDesign)
    {
        foreach (var child in entityDesign.Entity.Transform.Children)
        {
            if (!asset.Hierarchy.Parts.TryGetValue(child.Entity.Id, out var childDesign))
            {
                childDesign = new EntityDesign(child.Entity);
            }
            if (child.Entity != childDesign.Entity) throw new InvalidOperationException();
            yield return childDesign;
        }
    }
}
