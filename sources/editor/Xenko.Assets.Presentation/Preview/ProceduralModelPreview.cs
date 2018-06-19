// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Compiler;
using Xenko.Assets.Models;
using Xenko.Assets.Presentation.Preview.Views;
using Xenko.Editor.Preview;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview materials.
    /// </summary>
    [AssetPreview(typeof(ProceduralModelAsset), typeof(ModelPreviewView))]
    public class ProceduralModelPreview : PreviewFromEntity<ProceduralModelAsset>
    {
        /// <inheritdoc/>
        protected override PreviewEntity CreatePreviewEntity()
        {
            // load the material from the data base
            var model = LoadAsset<Model>(AssetItem.Location);

            // create the entity, create and set the model component
            var modelEntity = new Entity { Name = BuildName() };
            modelEntity.Add(new ModelComponent { Model = model });

            var previewEntity = new PreviewEntity(modelEntity);
            previewEntity.Disposed += () => UnloadAsset(model);
            return previewEntity;
        }
    }
}
