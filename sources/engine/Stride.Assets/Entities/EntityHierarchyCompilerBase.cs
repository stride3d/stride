// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Stride.Assets.Entities
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

                foreach (var component in entityData.Entity.Components)
                {
                    var type = component.GetType();
                    var componentName = type.Name; // QUESTION: Should we check attributes for another name?
                    var fields = type.GetFields(); // public fields, only those appear in GS?
                    var properties = type.GetRuntimeProperties(); // all properties may have a DataMember attribute
                    foreach(var field in fields)
                    {
                        if (field.FieldType.IsValueType)
                            continue; // value types cannot be null, and must always have a proper default value
                        
                        MemberRequiredAttribute memberRequired;
                        if ((memberRequired = field.GetCustomAttribute<MemberRequiredAttribute>()) != null)
                        {
                            if(field.GetValue(component) == null)
                            {
                                var logMsg = $"The component {componentName} on entity [{targetUrlInStorage}:{entityData.Entity.Name}] is missing a value on a required field '{field.Name}'.";
                                switch (memberRequired.ReportAs)
                                {
                                    case MemberRequiredReportType.Warning:
                                        result.Warning(logMsg);
                                        break;
                                    case MemberRequiredReportType.Error:
                                        result.Error(logMsg);
                                        break;
                                }
                            }
                        }
                    }
                    foreach (var prop in properties)
                    {
                        if (prop.PropertyType.IsValueType)
                            continue; // value types cannot be null, and must always have a proper default value

                        MemberRequiredAttribute memberRequired;
                        if ((memberRequired = prop.GetCustomAttribute<MemberRequiredAttribute>()) != null)
                        {
                            if (prop.GetValue(component) == null)
                            {
                                var logMsg = $"The component {componentName} on entity [{targetUrlInStorage}:{entityData.Entity.Name}] is missing a value on a required field '{prop.Name}'.";
                                switch (memberRequired.ReportAs)
                                {
                                    case MemberRequiredReportType.Warning:
                                        result.Warning(logMsg);
                                        break;
                                    case MemberRequiredReportType.Error:
                                        result.Error(logMsg);
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(Create(targetUrlInStorage, asset, assetItem.Package));
        }

        protected abstract AssetCommand<T> Create(string url, T assetParameters, Package package);
    }
}
