using System;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Stride.Assets.Entities.ComponentChecks
{
    /// <summary>
    /// Checks if the <see cref="ModelComponent"/> has a Model associated with it and that this Model has a reachable asset.
    /// </summary>
    public class ModelComponentCheck : IEntityComponentCheck
    {
        /// <inheritdoc/>
        public bool AppliesTo(Type componentType)
        {
            return componentType == typeof(ModelComponent);
        }

        /// <inheritdoc/>
        public void Check(EntityComponent component, Entity entity, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var modelComponent = component as ModelComponent;
            if (modelComponent.Model == null)
            {
                result.Warning($"The entity [{targetUrlInStorage}:{entity.Name}] has a model component that does not reference any model.");
            }
            else
            {
                var modelAttachedReference = AttachedReferenceManager.GetAttachedReference(modelComponent.Model);
                var modelId = modelAttachedReference.Id;

                // compute the full path to the source asset.
                var modelAssetItem = assetItem.Package.Session.FindAsset(modelId);
                if (modelAssetItem == null)
                {
                    result.Error($"The entity [{targetUrlInStorage}:{entity.Name}] is referencing an unreachable model.");
                }
            }
        }
    }
}
