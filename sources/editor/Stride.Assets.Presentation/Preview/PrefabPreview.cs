// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.Entities;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Editor.Preview;
using Stride.Engine;

namespace Stride.Assets.Presentation.Preview
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
