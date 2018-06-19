// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.Serialization;
using Xenko.Engine;

namespace Xenko.Assets.Entities
{
    public abstract class EntityHierarchyCompilerBase<T> : AssetCompilerBase where T : EntityHierarchyAssetBase
    {
        public override IEnumerable<Type> GetRuntimeTypes(AssetItem assetItem)
        {
            yield return typeof(Entity);
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (T)assetItem.Asset;
            foreach (var entityData in asset.Hierarchy.Parts.Values)
            {
                // TODO: How to make this code pluggable?
                var modelComponent = entityData.Entity.Components.Get<ModelComponent>();                
                if (modelComponent != null)
                {
                    if (modelComponent.Model == null)
                    {
                        result.Warning($"The entity [{targetUrlInStorage}:{entityData.Entity.Name}] has a model component that does not reference any model.");
                    }
                    else
                    {
                        var modelAttachedReference = AttachedReferenceManager.GetAttachedReference(modelComponent.Model);
                        var modelId = modelAttachedReference.Id;

                        // compute the full path to the source asset.
                        var modelAssetItem = assetItem.Package.Session.FindAsset(modelId);
                        if (modelAssetItem == null)
                        {
                            result.Error($"The entity [{targetUrlInStorage}:{entityData.Entity.Name}] is referencing an unreachable model.");
                        }
                    }
                }

                var nodeLinkComponent = entityData.Entity.Components.Get<ModelNodeLinkComponent>();
                if (nodeLinkComponent != null)
                {
                    nodeLinkComponent.ValidityCheck();
                    if (!nodeLinkComponent.IsValid)
                    {
                        result.Warning($"The Model Node Link between {entityData.Entity.Name} and {nodeLinkComponent.Target?.Entity.Name} is invalid.");
                        nodeLinkComponent.Target = null;
                    }
                }
            }

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(Create(targetUrlInStorage, asset, assetItem.Package));
        }

        protected abstract AssetCommand<T> Create(string url, T assetParameters, Package package);
    }
}
