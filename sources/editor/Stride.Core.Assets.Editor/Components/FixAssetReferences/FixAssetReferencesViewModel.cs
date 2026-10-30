// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.FixReferences;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Components.FixAssetReferences;

/// <summary>
/// The references to fix when assets that other assets reference are deleted: each one is replaced with a reference to another
/// asset, or cleared.
/// </summary>
public sealed class FixAssetReferencesViewModel : FixReferencesViewModel<AssetViewModel>
{
    private readonly SessionViewModel session;
    private readonly IAssetDependencyManager dependencyManager;
    private readonly HashSet<AssetId> hashIds;

    public FixAssetReferencesViewModel(IViewModelServiceProvider serviceProvider, SessionViewModel session, IEnumerable<AssetViewModel> deletedObjects, IAssetDependencyManager dependencyManager)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dependencyManager);
        this.session = session;
        this.dependencyManager = dependencyManager;
        hashIds = [.. deletedObjects.Select(x => x.Id)];
    }

    protected override IEnumerable<KeyValuePair<object, List<AssetViewModel>>> FindReferencers(AssetViewModel deletedObject)
    {
        var dependencies = dependencyManager.ComputeDependencies(deletedObject.AssetItem.Id, AssetDependencySearchOptions.In, ContentLinkType.Reference);
        var referencers = dependencies?.LinksIn.Select(x => session.GetAssetById(x.Item.Id)).NotNull().Where(x => !hashIds.Contains(x.Id)).ToList() ?? [];
        yield return new KeyValuePair<object, List<AssetViewModel>>(deletedObject, referencers);
    }

    protected override IEnumerable<ReferenceReplacementViewModel<AssetViewModel>> GetReplacementsForReferencer(AssetViewModel referencer, object referencedMember)
    {
        var rootNode = session.AssetNodeContainer.GetNode(referencer.Asset);
        var visitor = new GraphVisitorBase { SkipRootNode = true };
        var result = new List<ReferenceReplacementViewModel<AssetViewModel>>();
        visitor.Visiting += (node, path) =>
        {
            if (node is not IAssetMemberNode memberNode || !AssetRegistry.CanBeAssignedToContentTypes(memberNode.Descriptor.GetInnerCollectionType(), checkIsUrlType: true))
            {
                return;
            }

            if (memberNode.Target?.IsEnumerable ?? false)
            {
                foreach (var index in memberNode.Target.Indices)
                {
                    // If this property is inherited it will be updated by the standard propagation
                    if (memberNode.Target.IsItemInherited(index))
                        continue;

                    var target = ContentReferenceHelper.GetReferenceTarget(referencer.Session, memberNode.Target.Retrieve(index));
                    if (target == CurrentObjectToReplace)
                    {
                        // If so, prepare a replacement for it.
                        var viewModel = new AssetReferenceReplacementViewModel(this, CurrentObjectToReplace, referencer, memberNode.Target, index);
                        result.Add(viewModel);
                    }
                }
            }
            else
            {
                // If this property is inherited it will be updated by the standard propagation
                if (memberNode.IsContentInherited())
                    return;

                var target = ContentReferenceHelper.GetReferenceTarget(referencer.Session, memberNode.Retrieve());
                if (target == CurrentObjectToReplace)
                {
                    // If so, prepare a replacement for it.
                    var viewModel = new AssetReferenceReplacementViewModel(this, CurrentObjectToReplace, referencer, memberNode, NodeIndex.Empty);
                    result.Add(viewModel);
                }
            }
        };
        visitor.Visit(rootNode);
        return result;
    }

    public override async Task<AssetViewModel?> PickupObject(AssetViewModel objectToFix, Type propertyType)
    {
        IEnumerable<Type> acceptedTypes = AssetRegistry.CanPropertyHandleAssets(propertyType, out var assetTypes) ? assetTypes : [objectToFix.AssetType];
        return await ServiceProvider.Get<IEditorDialogService>().OpenAssetPickerAsync(
            session,
            acceptedTypes,
            x => !IsInObjectsToFixList(x),
            initialLocation: objectToFix.Directory,
            message: Translation.Tr._p("Message", "Select an asset to replace the deleted asset"));
    }
}
