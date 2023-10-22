// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

public sealed class EntityViewModel : EntityHierarchyItemViewModel
{
    private readonly MemberGraphNodeBinding<string> nameNodeBinding;

    public EntityViewModel(EntityHierarchyViewModel asset, EntityDesign entityDesign)
        : base(asset, GetOrCreateChildPartDesigns((EntityHierarchyAssetBase)asset.Asset, entityDesign))
    {
        EntityDesign = entityDesign;

        var assetNode = asset.Session.AssetNodeContainer.GetOrCreateNode(entityDesign.Entity);
        nameNodeBinding = new MemberGraphNodeBinding<string>(assetNode[nameof(Entity.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, ServiceProvider.TryGet<IUndoRedoService>());     
    }
    
    /// <inheritdoc/>
    public override string? Name
    {
        get => nameNodeBinding.Value;
        set => nameNodeBinding.Value = value;
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
