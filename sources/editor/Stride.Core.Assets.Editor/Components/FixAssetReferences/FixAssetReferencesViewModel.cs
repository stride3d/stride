// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.FixReferences;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.FixAssetReferences
{
    public class FixAssetReferencesViewModel : FixReferencesViewModel<AssetViewModel>
    {
        private readonly IAssetDependencyManager dependencyManager;
        private readonly HashSet<AssetId> hashIds;

        public FixAssetReferencesViewModel([NotNull] IViewModelServiceProvider serviceProvider, [ItemNotNull, NotNull] IEnumerable<AssetViewModel> deletedObjects, [NotNull] IAssetDependencyManager dependencyManager, [NotNull] IFixReferencesDialog dialog)
            : base(serviceProvider, dialog)
        {
            if (dependencyManager == null) throw new ArgumentNullException(nameof(dependencyManager));
            this.dependencyManager = dependencyManager;
            hashIds = new HashSet<AssetId>();
            hashIds.AddRange(deletedObjects.Select(x => x.Id));
        }

        protected override IEnumerable<KeyValuePair<object, List<AssetViewModel>>> FindReferencers(AssetViewModel deletedObject)
        {
            var session = deletedObject.Session;
            var dependencies = dependencyManager.ComputeDependencies(deletedObject.AssetItem.Id, AssetDependencySearchOptions.In, ContentLinkType.Reference);
            var referencers = dependencies?.LinksIn.Select(x => session.GetAssetById(x.Item.Id)).NotNull().Where(x => !hashIds.Contains(x.Id)).ToList() ?? new List<AssetViewModel>();
            yield return new KeyValuePair<object, List<AssetViewModel>>(deletedObject, referencers);
        }

        protected override IEnumerable<ReferenceReplacementViewModel<AssetViewModel>> GetReplacementsForReferencer(AssetViewModel referencer, object referencedMember)
        {
            var rootNode = SessionViewModel.Instance.AssetNodeContainer.GetNode(referencer.Asset);
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
                            var viewModel = new AssetReferenceReplacementViewModel(this, CurrentObjectToReplace, referencer, referencedMember, memberNode.Target, index);
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
                        var viewModel = new AssetReferenceReplacementViewModel(this, CurrentObjectToReplace, referencer, referencedMember, memberNode, NodeIndex.Empty);
                        result.Add(viewModel);
                    }
                }
            };
            visitor.Visit(rootNode);
            return result;
        }

        public override async Task<AssetViewModel> PickupObject(AssetViewModel objectToFix, Type propertyType)
        {
            var assetPicker = ServiceProvider.Get<IEditorDialogService>().CreateAssetPickerDialog(objectToFix.Session);
            assetPicker.Message = "Select an asset to replace the deleted asset";
            assetPicker.Filter = x => !IsInObjectsToFixList(x);
            assetPicker.InitialLocation = objectToFix.Directory;
            assetPicker.AllowMultiSelection = false;
            Type assetType = objectToFix.AssetType;

            if (AssetRegistry.CanPropertyHandleAssets(propertyType, out var assetTypes))
            {
                assetPicker.AcceptedTypes.AddRange(assetTypes);
            }
            else
            {
                assetPicker.AcceptedTypes.Add(assetType);
            }

            var result = await assetPicker.ShowModal();
            return result == DialogResult.Ok ? assetPicker.SelectedAssets.First() : null;
        }
    }
}
