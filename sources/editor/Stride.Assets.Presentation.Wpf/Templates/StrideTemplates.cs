// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum.Templates;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;

namespace Stride.Assets.Presentation.Templates
{
    internal static class StrideTemplates
    {
        public static void Register()
        {
            // TODO: Attribute-based auto registration would be better I guess
            // SessionTemplateGenerator-derived generators are wrapped with the Quantum-aware
            // decorator at registration time so override metadata is materialized through the
            // asset graph before save. CLI / headless flows (Samples Tests, sample regen)
            // register session generators directly via TemplateManager.Register and skip the
            // wrap.
            QuantumTemplateRegistration.Register(new DotNetNewTemplateGenerator(new WpfDotNetNewParameterPrompt()));
            // AddLibraryGenerator registered after DotNetNew so it takes precedence for stride-library
            // templates (TemplateManager iterates most-recent-first).
            QuantumTemplateRegistration.Register(new AddLibraryGenerator(new WpfAddLibraryParameterPrompt()));
            TemplateManager.Register(new UpdatePlatformsGenerator(new WpfUpdatePlatformsParameterPrompt()));
            RegisterUpdatePlatformsDescription();
            TemplateManager.Register(AssetFactoryTemplateGenerator.Default);
            TemplateManager.Register(AssetFromFileTemplateGenerator.Default);
            // Specific asset templates must be registered after AssetFactoryTemplateGenerator
            TemplateManager.Register(HeightmapFactoryTemplateGenerator.Default);
            TemplateManager.Register(ColliderShapeHullFactoryTemplateGenerator.Default);
            TemplateManager.Register(ColliderShapeStaticMeshFactoryTemplateGenerator.Default);
            TemplateManager.Register(HullAssetFactoryTemplateGenerator.Default);
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

        /// <summary>
        /// Synthetic "Update platforms" TemplateDescription — UpdatePlatformsGenerator drives stride-game with updateOnly=true at runtime; no dotnet new template backs it.
        /// </summary>
        private static void RegisterUpdatePlatformsDescription()
        {
            var description = new TemplateDescription
            {
                Id = UpdatePlatformsGenerator.TemplateId,
                Name = "Update platforms",
                Description = "Add or update per-platform executable projects for the current game",
                Group = "General",
                Scope = TemplateScope.Package,
                DefaultOutputName = "MyGame",
                FullPath = new UFile("Stride.UpdatePlatforms.synthetic.sdtpl"),
            };
            // Wrapped in a synthetic Package because TemplateManager.FindTemplates iterates
            // packages.SelectMany(p => p.Templates).
            var package = new Package { FullPath = new UFile("Stride.UpdatePlatforms.synthetic") };
            package.Templates.Add(description);
            TemplateManager.RegisterPackage(package);
        }
    }
}
