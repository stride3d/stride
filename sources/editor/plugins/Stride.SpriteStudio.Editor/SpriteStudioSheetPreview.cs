// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.Preview;
using Stride.Core.IO;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.SpriteStudio.Assets;
using Stride.SpriteStudio.Runtime;

namespace Stride.SpriteStudio.Editor;

/// <summary>
/// Previews a SpriteStudio sheet on an entity; the view is the model preview's.
/// </summary>
[AssetPreview<SpriteStudioModelAsset>]
public class SpriteStudioSheetPreview : PreviewFromEntity<SpriteStudioModelAsset>
{
    /// <inheritdoc/>
    protected override PreviewEntity CreatePreviewEntity()
    {
        UFile spriteStudioSheetLocation = AssetItem.Location;
        var sheet = LoadAsset<SpriteStudioSheet>(spriteStudioSheetLocation);

        var entity = new Entity { Name = "Preview entity of SpriteStudio sheet: " + spriteStudioSheetLocation };
        entity.Add(new SpriteStudioComponent { Sheet = sheet });

        var previewEntity = new PreviewEntity(entity);
        previewEntity.Disposed += () => UnloadAsset(sheet);
        return previewEntity;
    }
}
