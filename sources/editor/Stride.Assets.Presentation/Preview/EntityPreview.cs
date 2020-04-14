// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.Entities;
using Stride.Editor.Preview;
using Stride.Engine;

namespace Stride.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview entities.
    /// </summary>
    // DO NOT REACTIVATE THIS PREVIEW WITHOUT MAKING A DISTINCT PREVIEW BETWEEN ENTITIES AND SCENE! SCENE IS LOADED (AND NOW UNLOADED) at initialization, we absolutely don't want to do that
    //[AssetPreview(typeof(PrefabAsset), typeof(EntityPreviewView))]
    public class EntityPreview : PreviewFromEntity<PrefabAsset>
    {        
        /// <inheritdoc/>
        protected override PreviewEntity CreatePreviewEntity()
        {
            // create the preview entity from the entity build on the database
            var entity = LoadAsset<Entity>(AssetItem.Location);
            var previewEntity = new PreviewEntity(entity);

            // ensure that the model is correctly unloaded after used
            previewEntity.Disposed += () => UnloadAsset(previewEntity.Entity);

            return previewEntity;
        }
    }
}
