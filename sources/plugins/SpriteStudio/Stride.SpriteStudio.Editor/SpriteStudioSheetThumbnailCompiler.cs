// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Editor.Thumbnails;
using Stride.Engine;
using Stride.Rendering.Compositing;
using Stride.SpriteStudio.Assets;
using Stride.SpriteStudio.Runtime;

namespace Stride.SpriteStudio.Editor;

[AssetCompiler(typeof(SpriteStudioModelAsset), typeof(ThumbnailCompilationContext))]
public class SpriteStudioSheetThumbnailCompiler : ThumbnailCompilerBase<SpriteStudioModelAsset>
{
    public SpriteStudioSheetThumbnailCompiler()
    {
        IsStatic = false;
    }

    protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
    {
        result.BuildSteps.Add(new ThumbnailBuildStep(new SpriteStudioSheetThumbnailCommand(context, thumbnailStorageUrl, assetItem, originalPackage,
            new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
    }

    /// <summary>
    /// Renders the sheet on an entity facing the camera.
    /// </summary>
    public class SpriteStudioSheetThumbnailCommand : ThumbnailFromEntityCommand<SpriteStudioSheet>
    {
        public SpriteStudioSheetThumbnailCommand(ThumbnailCompilerContext context, string url, AssetItem spriteStudioSheetItem, IAssetFinder assetFinder, ThumbnailCommandParameters description)
            : base(context, spriteStudioSheetItem, assetFinder, url, description)
        {
        }

        protected override CameraComponent CreateCamera(GraphicsCompositor graphicsCompositor)
        {
            var camera = base.CreateCamera(graphicsCompositor);
            // Reset rotation, we want to be facing camera
            camera.Entity.Transform.Rotation = Quaternion.Identity;
            return camera;
        }

        protected override Entity CreateEntity()
        {
            var entity = new Entity { Name = "Thumbnail Entity of model: " + AssetUrl };
            entity.Add(new SpriteStudioComponent { Sheet = LoadedAsset });
            return entity;
        }

        protected override void AdjustEntity()
        {
            base.AdjustEntity();

            // Reset rotation
            Entity.Transform.Rotation = Quaternion.Identity;
        }
    }
}
