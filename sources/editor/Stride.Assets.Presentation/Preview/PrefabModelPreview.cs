// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.IO;
using Xenko.Assets.Models;
using Xenko.Assets.Presentation.Preview.Views;
using Xenko.Editor.Preview;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.Preview
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
