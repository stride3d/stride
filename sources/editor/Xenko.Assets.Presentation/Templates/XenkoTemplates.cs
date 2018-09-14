// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Templates;

namespace Xenko.Assets.Presentation.Templates
{
    internal static class XenkoTemplates
    {
        public static void Register()
        {
            // TODO: Attribute-based auto registration would be better I guess
            TemplateManager.Register(TemplateSampleGenerator.Default);
            TemplateManager.Register(NewGameTemplateGenerator.Default);
            TemplateManager.Register(ProjectLibraryTemplateGenerator.Default);
            TemplateManager.Register(UpdatePlatformsTemplateGenerator.Default);
            TemplateManager.Register(AssetFactoryTemplateGenerator.Default);
            TemplateManager.Register(AssetFromFileTemplateGenerator.Default);
            // Specific asset templates must be registered after AssetFactoryTemplateGenerator
            TemplateManager.Register(ColliderShapeHullFactoryTemplateGenerator.Default);
            TemplateManager.Register(ProceduralModelFactoryTemplateGenerator.Default);
            TemplateManager.Register(SkyboxFactoryTemplateGenerator.Default);
            TemplateManager.Register(GraphicsCompositorTemplateGenerator.Default);
            TemplateManager.Register(ScriptTemplateGenerator.Default);
            TemplateManager.Register(SpriteSheetFromFileTemplateGenerator.Default);
            TemplateManager.Register(ModelFromFileTemplateGenerator.Default);
            TemplateManager.Register(SkeletonFromFileTemplateGenerator.Default);
            TemplateManager.Register(AnimationFromFileTemplateGenerator.Default);
            TemplateManager.Register(VideoFromFileTemplateGenerator.Default);
            TemplateManager.Register(SoundFromFileTemplateGenerator.Default);
        }
    }
}
