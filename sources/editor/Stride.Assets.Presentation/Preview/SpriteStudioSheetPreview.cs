// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.IO;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.SpriteStudio.Offline;
using Stride.SpriteStudio.Runtime;

namespace Stride.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview models.
    /// </summary>
    // FIXME: this view model should be in the SpriteStudio offline assembly! Can't be done now, because of a circular reference in CompilerApp referencing SpriteStudio, and Editor referencing CompilerApp
    [AssetPreview(typeof(SpriteStudioModelAsset), typeof(ModelPreviewView))]
    public class SpriteStudioSheetPreview : PreviewFromEntity<SpriteStudioModelAsset>
    {
        /// <inheritdoc/>
        protected override PreviewEntity CreatePreviewEntity()
        {
            UFile spriteStudioSheetLocation = AssetItem.Location;
            // load the created material and the model from the data base
            var sheet = LoadAsset<SpriteStudioSheet>(spriteStudioSheetLocation);

            // create the entity, create and set the model component
            var entity = new Entity { Name = "Preview entity of SpriteStudio sheet: " + spriteStudioSheetLocation };
            entity.Add(new SpriteStudioComponent { Sheet = sheet });

            var previewEntity = new PreviewEntity(entity);

            previewEntity.Disposed += () => UnloadAsset(sheet);

            return previewEntity;
        }
    }
}
