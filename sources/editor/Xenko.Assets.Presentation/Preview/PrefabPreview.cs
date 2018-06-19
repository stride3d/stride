// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.Preview.Views;
using Xenko.Editor.Preview;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.Preview
{
    [AssetPreview(typeof(PrefabAsset), typeof(ModelPreviewView))]
    public class PrefabPreview : PreviewFromEntity<PrefabAsset>
    {
        protected override PreviewEntity CreatePreviewEntity()
        {
            var prefab = LoadAsset<Prefab>(AssetItem.Location);

            var entity = new Entity { Name = "Preview Entity of model: " + AssetItem.Location };

            foreach (var prefabEntity in prefab.Entities)
            {
                entity.AddChild(prefabEntity);
            }

            var previewEntity = new PreviewEntity(entity);
            previewEntity.Disposed += () => UnloadAsset(prefab);

            return previewEntity;
        }
    }
}
