// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Assets.Presentation.AssetEditors.UIEditor.Services;
using Xenko.Assets.UI;
using Xenko.Editor.Thumbnails;
using Xenko.Engine;
using Xenko.Engine.Processors;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Presentation.Thumbnails
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
