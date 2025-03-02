// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Engine;

namespace Stride.Assets.Editor.Preview;

[AssetPreview<PrefabAsset>]
public class PrefabPreview : PreviewFromEntity<PrefabAsset>
{
    protected override PreviewEntity CreatePreviewEntity()
    {
        var entity = new Entity { Name = "Preview Entity of model: " + AssetItem.Location };

        var prefab = LoadAsset<Prefab>(AssetItem.Location);
        if (prefab != null)
        {
            foreach (var prefabEntity in prefab.Entities)
            {
                entity.AddChild(prefabEntity);
            }
        }
        
        var previewEntity = new PreviewEntity(entity);
        previewEntity.Disposed += () => UnloadAsset(prefab);
        return previewEntity;
    }
}
