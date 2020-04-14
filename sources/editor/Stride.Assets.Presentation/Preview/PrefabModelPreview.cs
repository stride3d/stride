// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.IO;
using Stride.Assets.Models;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview prefab models.
    /// </summary>
    [AssetPreview(typeof(PrefabModelAsset), typeof(ModelPreviewView))]
    public class PrefabModelPreview : PreviewFromEntity<PrefabModelAsset>
    {
        /// <inheritdoc/>
        protected override PreviewEntity CreatePreviewEntity()
        {
            UFile modelLocation = AssetItem.Location;
            // load the created material and the model from the data base
            var model = LoadAsset<Model>(modelLocation);

            // create the entity, create and set the model component
            var entity = new Entity { Name = "Preview Entity of model: " + modelLocation };
            entity.Add(new ModelComponent { Model = model });

            var previewEntity = new PreviewEntity(entity);

            previewEntity.Disposed += () => UnloadAsset(model);

            return previewEntity;
        }
    }
}
