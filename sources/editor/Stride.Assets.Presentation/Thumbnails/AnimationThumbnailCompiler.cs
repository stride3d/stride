// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Animations;
using Stride.Assets.Models;
using Stride.Assets.Presentation.Resources.Thumbnails;
using Stride.Assets.Presentation.ViewModel.Preview;
using Stride.Editor.Resources;
using Stride.Editor.Thumbnails;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(AnimationAsset), typeof(ThumbnailCompilationContext))]
    public class AnimationThumbnailCompiler : ThumbnailCompilerBase<AnimationAsset>
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(ModelAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
            yield return new BuildDependencyInfo(typeof(AnimationAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
        }

        public AnimationThumbnailCompiler()
        {
            IsStatic = false;
            Priority = 12000;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            // A model asset should have been generated during CompileWithDependencies()
            var modelAssetItem = AnimationPreviewViewModel.FindModelForPreview(assetItem);

            if (modelAssetItem != null)
            {
                result.BuildSteps.Add(new ThumbnailBuildStep(new AnimationThumbnailBuildCommand(context, thumbnailStorageUrl, assetItem, modelAssetItem, originalPackage,
                    new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
            }
            else
            {
                // If no model could be found, uses default thumbnail instead
                var gameSettings = context.GetGameSettingsAsset();
                result.BuildSteps.Add(new StaticThumbnailCommand<AnimationAsset>(thumbnailStorageUrl, StaticThumbnails.AnimationThumbnail, context.ThumbnailResolution, gameSettings.GetOrCreate<RenderingSettings>().ColorSpace == ColorSpace.Linear, assetItem.Package));
            }
        }

        /// <summary>
        /// Command used to build the thumbnail of the texture in the storage
        /// </summary>
        private class AnimationThumbnailBuildCommand : ThumbnailFromEntityCommand<AnimationClip>
        {
            private readonly AssetItem modelItem;
            private Model model;
            private AnimationClip srcClip;
            private Texture animationPreviewTexture;

            public AnimationThumbnailBuildCommand(ThumbnailCompilerContext context, string url, AssetItem animationItem, AssetItem modelItem, IAssetFinder assetFinder, ThumbnailCommandParameters description)
                : base(context, animationItem, assetFinder, url, description)
            {
                this.modelItem = modelItem;
            }

            protected override Entity CreateEntity()
            {
                // create the entity, create and set the model component
                var entity = new Entity { Name = "Thumbnail Entity of model: " + AssetUrl };
                entity.Add(new ModelComponent { Model = model });
                entity.Add(new AnimationComponent { Animations = { { "preview", srcClip ?? LoadedAsset } } });

                if (srcClip != null || LoadedAsset != null)
                    entity.Get<AnimationComponent>().Play("preview");

                return entity;
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);

                // Also process Model
                var dependencies = modelItem.Package.Session.DependencyManager.ComputeDependencies(modelItem.Id, AssetDependencySearchOptions.Out | AssetDependencySearchOptions.Recursive, ContentLinkType.Reference);
                if (dependencies != null)
                {
                    foreach (var assetReference in dependencies.LinksOut)
                    {
                        var refAsset = assetReference.Item.Asset;
                        writer.SerializeExtended(ref refAsset, ArchiveMode.Serialize);
                    }
                }
            }

            protected override void CustomizeThumbnail(Image image)
            {
                base.CustomizeThumbnail(image);

                // Combine textures
                using (var thumbnailBuilderHelper = new ThumbnailBuildHelper())
                using (var generatedThumbnail = Texture.New(thumbnailBuilderHelper.GraphicsDevice, image))
                {
                    // Set color space
                    var oldColorSpace = thumbnailBuilderHelper.GraphicsDevice.ColorSpace;
                    thumbnailBuilderHelper.GraphicsDevice.ColorSpace = Parameters.ColorSpace;

                    // Load animation default thumbnail
                    if (animationPreviewTexture == null)
                        animationPreviewTexture = TextureExtensions.FromFileData(thumbnailBuilderHelper.GraphicsDevice, StaticThumbnails.AnimationThumbnail);

                    thumbnailBuilderHelper.InitializeRenderTargets(PixelFormat.R8G8B8A8_UNorm_SRgb, animationPreviewTexture.Width, animationPreviewTexture.Height);

                    // Generate thumbnail with status icon
                    // Clear (transparent)
                    thumbnailBuilderHelper.GraphicsContext.CommandList.Clear(thumbnailBuilderHelper.RenderTarget, new Color4());
                    thumbnailBuilderHelper.GraphicsContext.CommandList.SetRenderTargetAndViewport(null, thumbnailBuilderHelper.RenderTarget);

                    // Render thumbnail and status sprite
                    thumbnailBuilderHelper.SpriteBatch.Begin(thumbnailBuilderHelper.GraphicsContext);
                    thumbnailBuilderHelper.SpriteBatch.Draw(animationPreviewTexture, Vector2.Zero, new Color(0xFF, 0xFF, 0xFF, 0x20));
                    thumbnailBuilderHelper.SpriteBatch.Draw(generatedThumbnail, Vector2.Zero);
                    thumbnailBuilderHelper.SpriteBatch.End();

                    thumbnailBuilderHelper.GraphicsDevice.ColorSpace = oldColorSpace;

                    // Read back result to image
                    thumbnailBuilderHelper.RenderTarget.GetData(thumbnailBuilderHelper.GraphicsContext.CommandList, thumbnailBuilderHelper.RenderTargetStaging, new DataPointer(image.PixelBuffer[0].DataPointer, image.PixelBuffer[0].BufferStride));
                    image.Description.Format = thumbnailBuilderHelper.RenderTarget.Format; // In case channels are swapped
                }
            }

            protected override void PreloadAsset()
            {
                base.PreloadAsset();
                model = Generator.ContentManager.Load<Model>(modelItem.Location);
                // In case of difference animation, we don't want to display the meaningless diff but apply it on top of the base animation.
                if (((AnimationAsset)Parameters.Asset).Type.GetType() == typeof(DifferenceAnimationAssetType))
                {
                    srcClip = Generator.ContentManager.Load<AnimationClip>(AssetUrl + AnimationAssetCompiler.SrcClipSuffix);
                }
            }

            protected override void UnloadAsset()
            {
                UnloadAsset(ref model);
                if (srcClip != null) UnloadAsset(ref srcClip);
                base.UnloadAsset();
            }
        }
    }
}
