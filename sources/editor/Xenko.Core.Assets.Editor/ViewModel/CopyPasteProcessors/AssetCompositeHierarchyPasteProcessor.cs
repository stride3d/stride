// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Quantum.Visitors;
using Xenko.Core.Assets.Serializers;
using Xenko.Core.Assets.Yaml;
using Xenko.Core;
using Xenko.Core.Reflection;

namespace Xenko.Core.Assets.Editor.ViewModel.CopyPasteProcessors
{
    public class AssetCompositeHierarchyPasteProcessor<TAssetPartDesign, TAssetPart> : PasteProcessorBase
        where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
        where TAssetPart : class, IIdentifiable
    {
        /// <inheritdoc />
        public override bool Accept(Type targetRootType, Type targetMemberType, Type pastedDataType)
        {
            return typeof(AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>).IsAssignableFrom(targetRootType) &&
                   (typeof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>).IsAssignableFrom(targetMemberType) || typeof(TAssetPart).IsAssignableFrom(targetMemberType)) &&
                   typeof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>).IsAssignableFrom(pastedDataType);
        }

        /// <inheritdoc />
        public override bool ProcessDeserializedData(AssetPropertyGraphContainer graphContainer, object targetRootObject, Type targetMemberType, ref object data, bool isRootDataObjectReference, AssetId? sourceId, YamlAssetMetadata<OverrideType> overrides, YamlAssetPath basePath)
        {
            var asset = (AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>)targetRootObject;
            var hierarchy = data as AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>;
            if (hierarchy == null)
                return false;

            // Create a temporary asset to host the hierarchy to paste, so we have a property graph to manipulate it.
            var tempAsset = (AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>)Activator.CreateInstance(asset.GetType());
            tempAsset.Hierarchy = hierarchy;
            // Use temporary containers so that any created nodes are discarded after the processing.
            var tempNodeContainer = new AssetNodeContainer { NodeBuilder = {NodeFactory = new AssetNodeFactory()} };
            var definition = AssetQuantumRegistry.GetDefinition(asset.GetType());
            var rootNode = tempNodeContainer.GetOrCreateNode(tempAsset);

            // If different asset or if at least one part already exists, create a custom clone.
            if (asset.Id != sourceId || hierarchy.Parts.Values.Any(part => asset.ContainsPart(part.Part.Id)))
            {
                // Clone again to create new ids for any IIdentifiable, but keep references to external object intact.
                var cloneExternalReferences = ExternalReferenceCollector.GetExternalReferences(definition, rootNode);
                hierarchy = AssetCloner.Clone(hierarchy, AssetClonerFlags.GenerateNewIdsForIdentifiableObjects, cloneExternalReferences, out var idRemapping);
                // Remap indices of parts in Hierarchy.Part
                AssetCloningHelper.RemapIdentifiablePaths(overrides, idRemapping, basePath);
                // Make new base instances ids in case the part are inherited.
                AssetPartsAnalysis.GenerateNewBaseInstanceIds(hierarchy);
                // Update the temporary asset with this cloned hierarchy.
                rootNode[nameof(AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>.Hierarchy)].Update(hierarchy);
            }

            // Collect all referenceable objects from the target asset (where we're pasting)
            var targetPropertyGraph = graphContainer.TryGetGraph(asset.Id);
            var referenceableObjects = IdentifiableObjectCollector.Collect(targetPropertyGraph.Definition, targetPropertyGraph.RootNode);

            // Replace references in the hierarchy being pasted by the real objects from the target asset.
            var externalReferences = new HashSet<Guid>(ExternalReferenceCollector.GetExternalReferences(definition, rootNode).Select(x => x.Id));
            var visitor = new ObjectReferencePathGenerator(definition)
            {
                ShouldOutputReference = x => externalReferences.Contains(x)
            };
            visitor.Visit(rootNode);

            FixupObjectReferences.FixupReferences(tempAsset, visitor.Result, referenceableObjects, true);

            data = hierarchy;

            return true;
        }
    }
}
