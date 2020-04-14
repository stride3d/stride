// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Services;
using Stride.Assets.UI;
using Stride.Editor.Thumbnails;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.Thumbnails
{
    /// <summary>
    /// Thumbnail compiler for the <see cref="UIPageAsset"/>.
    /// </summary>
    [AssetCompiler(typeof(UIPageAsset), typeof(ThumbnailCompilationContext))]
    public class UIPageThumbnailCompiler : ThumbnailCompilerBase<UIPageAsset>
    {
        public UIPageThumbnailCompiler()
        {
            IsStatic = false;
            Priority = 11000;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new ThumbnailBuildStep(new UIThumbnailBuildCommand(context, thumbnailStorageUrl, Asset.Design.Resolution, assetItem, originalPackage,
                new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
        }

        public class UIThumbnailBuildCommand : ThumbnailFromEntityCommand<UIPage>
        {
            private readonly Vector3 designResolution;

            public UIThumbnailBuildCommand(ThumbnailCompilerContext context, string url, Vector3 designResolution, AssetItem uiPageItem, IAssetFinder assetFinder, ThumbnailCommandParameters description)
                : base(context, uiPageItem, assetFinder, url, description)
            {
                this.designResolution = designResolution;
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);
                writer.Write(designResolution);
            }

            protected override CameraComponent CreateCamera(GraphicsCompositor graphicsCompositor)
            {
                var camera = base.CreateCamera(graphicsCompositor);
                var renderingSize = designResolution / UIEditorController.DesignDensity;

                // Use an orthographic camera
                camera.Projection = CameraProjectionMode.Orthographic;
                camera.OrthographicSize = Math.Max(renderingSize.X, renderingSize.Y);

                return camera;
            }

            protected override void AdjustEntity()
            {
                // Do not adjust entity using bounding sphere
            }

            protected override Entity CreateEntity()
            {
                // create the entity, create and set the model component
                var entity = new Entity { Name = "Thumbnail Entity of model: " + AssetUrl };
                entity.Add(new UIComponent
                {
                    Page = LoadedAsset,
                    Resolution = designResolution,
                    Size = designResolution / UIEditorController.DesignDensity,
                    IsFullScreen = false,
                });

                return entity;
            }
        }
    }
}
