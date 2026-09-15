// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Tests the 3.1.0.2 upgrade of the particle render feature in derived compositors.
    /// </summary>
    public class TestGraphicsCompositorUpgrader
    {
        private const string ParticleFeature = "9013eab3ea0ef6c98bf133b86c173d45";
        private const string MeshFeature = "5eeae53de8f0e5f60ea7d76afd46b39f";
        private const string Level10 = "823a81bf-bac0-4552-9267-aeed499c40df:DefaultGraphicsCompositorLevel10";
        private const string Level9 = "9af53371-51ba-49fc-b420-ee7874892e75:/Stride.Engine/DefaultGraphicsCompositorLevel9";

        [Theory]
        [InlineData(Level10)]
        [InlineData(Level9)]
        [InlineData("472e6944-3ddc-47db-8d6f-2e0a987475ec:/Stride.Voxels/DefaultGraphicsCompositorVoxels")]
        public void DerivedCompositorOwnsItsParticleFeature(string archetype)
        {
            var upgraded = Upgrade(Compositor(archetype, particleKey: ParticleFeature));

            AssertContains($"{ParticleFeature}*: !Stride.Particles.Rendering.ParticleEmitterRenderFeature,Stride.Particles", upgraded);
            AssertContains($"{MeshFeature}: !Stride.Rendering.MeshRenderFeature,Stride.Rendering", upgraded);
            AssertContains("SerializedVersion: {Stride: 3.1.0.2}", upgraded);
        }

        [Fact]
        public void OwnedParticleFeatureIsLeftAlone()
        {
            var upgraded = Upgrade(Compositor(Level10, particleKey: ParticleFeature + "*"));

            AssertContains($"{ParticleFeature}*: !Stride.Particles", upgraded);
            AssertDoesNotContain($"{ParticleFeature}**", upgraded);
        }

        [Fact]
        public void SealedParticleFeatureIsOwnedAndStaysSealed()
        {
            var upgraded = Upgrade(Compositor(Level10, particleKey: ParticleFeature + "!"));

            AssertContains($"{ParticleFeature}*!: !Stride.Particles", upgraded);
        }

        [Fact]
        public void CompositorWithoutArchetypeIsLeftAlone()
        {
            var upgraded = Upgrade(Compositor(archetype: null, particleKey: ParticleFeature));

            AssertContains($"{ParticleFeature}: !Stride.Particles", upgraded);
            AssertContains("SerializedVersion: {Stride: 3.1.0.2}", upgraded);
        }

        [Fact]
        public void ProjectWithParticlesOwnsTheFeature()
        {
            var upgraded = Upgrade(Compositor(Level10, particleKey: ParticleFeature), dependencies: ["Stride.Engine", "Stride.Particles"]);

            AssertContains($"{ParticleFeature}*: !Stride.Particles", upgraded);
        }

        [Fact]
        public void ProjectWithUnresolvedDependenciesKeepsTheFeature()
        {
            // A failed restore leaves the dependencies empty
            var upgraded = Upgrade(Compositor(Level10, particleKey: ParticleFeature), dependencies: []);

            AssertContains($"{ParticleFeature}*: !Stride.Particles", upgraded);
        }

        [Fact]
        public void DependencyPackageKeepsTheFeatures()
        {
            // A package the game uses (from NuGet) keeps the item
            var upgraded = Upgrade(Compositor(Level10, particleKey: ParticleFeature), dependencies: ["Stride.Engine"], packageName: "SomeLibrary", dependencyPackage: true);

            AssertContains($"{ParticleFeature}*: !Stride.Particles", upgraded);
        }

        [Fact]
        public void PluginPackageKeepsItsOwnFeature()
        {
            var upgraded = Upgrade(Compositor(Level10, particleKey: ParticleFeature), dependencies: ["Stride.Engine"], packageName: "Stride.Particles");

            AssertContains($"{ParticleFeature}*: !Stride.Particles", upgraded);
        }

        [Fact]
        public void DependencyNamesIgnoreCase()
        {
            var upgraded = Upgrade(Compositor(Level10, particleKey: ParticleFeature), dependencies: ["stride.engine", "stride.particles"]);

            AssertContains($"{ParticleFeature}*: !Stride.Particles", upgraded);
        }

        [Fact]
        public void ProjectWithoutParticlesDropsTheFeature()
        {
            var upgraded = Upgrade(Compositor(Level10, particleKey: ParticleFeature), dependencies: ["Stride.Engine"]);

            AssertDoesNotContain("ParticleEmitterRenderFeature", upgraded);
            AssertContains($"{MeshFeature}: !Stride.Rendering.MeshRenderFeature,Stride.Rendering", upgraded);
            AssertContains("SerializedVersion: {Stride: 3.1.0.2}", upgraded);
        }

        [Fact]
        public void CompositorDerivedFromAnotherCompositorFollowsItsBase()
        {
            // The base, derived from an engine default, owns the item after its own upgrade
            var upgraded = Upgrade(Compositor("c73fddfe-01b7-4fe0-ab60-51f001463388:GraphicsCompositor", particleKey: ParticleFeature));

            AssertContains($"{ParticleFeature}: !Stride.Particles", upgraded);
            AssertContains("SerializedVersion: {Stride: 3.1.0.2}", upgraded);
        }

        private static void AssertContains(string expected, string text)
            => Assert.True(text.Contains(expected, StringComparison.Ordinal), $"Expected to find:{Environment.NewLine}{expected}{Environment.NewLine}in:{Environment.NewLine}{text}");

        private static void AssertDoesNotContain(string unexpected, string text)
            => Assert.False(text.Contains(unexpected, StringComparison.Ordinal), $"Did not expect to find:{Environment.NewLine}{unexpected}{Environment.NewLine}in:{Environment.NewLine}{text}");

        private static string Compositor(string archetype, string particleKey)
        {
            var builder = new StringBuilder();
            builder.AppendLine("!GraphicsCompositorAsset");
            builder.AppendLine("Id: b346de3e-0e4b-4ee8-ab7a-5d75b37fe309");
            builder.AppendLine("SerializedVersion: {Stride: 3.1.0.1}");
            builder.AppendLine("Tags: []");
            if (archetype is not null)
                builder.AppendLine($"Archetype: {archetype}");
            builder.AppendLine("RenderFeatures:");
            builder.AppendLine($"    {MeshFeature}: !Stride.Rendering.MeshRenderFeature,Stride.Rendering");
            builder.AppendLine("        RenderStageSelectors: {}");
            builder.AppendLine($"    {particleKey}: !Stride.Particles.Rendering.ParticleEmitterRenderFeature,Stride.Particles");
            builder.AppendLine("        RenderStageSelectors: {}");
            return builder.ToString();
        }

        // Without dependencies, the asset is read outside a package
        private static string Upgrade(string yaml, string[] dependencies = null, string packageName = "MyGame.Game", bool dependencyPackage = false)
        {
            var directory = Path.Combine(Path.GetTempPath(), "StrideTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "GraphicsCompositor.sdgfxcomp");
                File.WriteAllText(path, yaml);

                Package package = null;
                if (dependencies != null)
                {
                    package = new Package { Meta = { Name = packageName } };
                    PackageContainer container = dependencyPackage
                        ? new StandalonePackage(package) { IsDependencyPackage = true }
                        : new SolutionProject(package, Guid.NewGuid(), Path.Combine(directory, $"{packageName}.csproj"));
                    foreach (var dependency in dependencies)
                        container.FlattenedDependencies.Add(new Dependency(dependency, new PackageVersion("4.5.0"), DependencyType.Package));
                }

                var logger = new LoggerResult();
                var file = new PackageLoadingAssetFile(path, directory);
                var context = new AssetMigrationContext(package, file.ToReference(), file.FilePath.ToOSPath(), logger);
                Assert.True(AssetMigration.MigrateAssetIfNeeded(context, file, "Stride"), "The compositor should need the upgrade.");
                Assert.False(logger.HasErrors, logger.ToText());
                return Encoding.UTF8.GetString(file.AssetContent);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
