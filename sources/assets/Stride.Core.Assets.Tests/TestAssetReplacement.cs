// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xunit;
using Stride.Core;
using Stride.Core.Assets.Analysis;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests
{
    public class TestAssetReplacement
    {
        private static (PackageSession Session, Package Game, Package Plugin) CreateSessionWithPlugin()
        {
            var session = new PackageSession();

            var pluginPackage = new Package();
            pluginPackage.Meta.Name = "Plugin";
            session.Projects.Add(new StandalonePackage(pluginPackage) { IsDependencyPackage = true });

            var gamePackage = new Package();
            var game = new SolutionProject(gamePackage, Guid.NewGuid(), "MyGame.csproj") { AssetNamespace = "MyGame" };
            session.Projects.Add(game);
            game.FlattenedDependencies.Add(new Dependency(pluginPackage));

            return (session, gamePackage, pluginPackage);
        }

        private static Package AddPlugin(Package gamePackage, string name)
        {
            var pluginPackage = new Package();
            pluginPackage.Meta.Name = name;
            gamePackage.Session.Projects.Add(new StandalonePackage(pluginPackage) { IsDependencyPackage = true });
            gamePackage.Container.FlattenedDependencies.Add(new Dependency(pluginPackage));
            return pluginPackage;
        }

        [Fact]
        public void TestReplacementCollectedAndSubstituted()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var target = new AssetItem("Logo", new AssetObjectTest { Name = "Original" });
            plugin.Assets.Add(target);
            var targetId = target.Id;
            var replacement = new AssetItem("Overrides/Logo", new AssetObjectTest { Name = "Replacement", Replaces = new UFile("/Plugin/Logo") });
            game.Assets.Add(replacement);

            var logger = new LoggerResult();
            Assert.True(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out var replacements));
            Assert.False(logger.HasErrors);
            var entry = Assert.Single(replacements);
            Assert.Equal(targetId, entry.Target.Id);
            Assert.Equal(replacement.Id, entry.Replacement.Id);

            AssetReplacementAnalysis.Substitute(replacements);

            // The replaced asset keeps its identity (id and location) but carries the replacement's content
            var substituted = game.FindAsset(new UFile("/Plugin/Logo"));
            Assert.NotNull(substituted);
            Assert.Equal(targetId, substituted.Id);
            Assert.Equal(targetId, substituted.Asset.Id);
            Assert.Equal("Replacement", ((AssetObjectTest)substituted.Asset).Name);
            Assert.NotSame(replacement.Asset, substituted.Asset);
            // The clone must not carry the declaration (it would point at its own location)
            Assert.Null(substituted.Asset.Replaces);
            // The replacement asset itself is untouched at its own URL
            var replacementItem = game.FindAsset(new UFile("/MyGame/Overrides/Logo"));
            Assert.NotNull(replacementItem);
            Assert.Equal(replacement.Id, replacementItem.Id);
        }

        [Fact]
        public void TestReplacementMissingTargetFails()
        {
            var (_, game, _) = CreateSessionWithPlugin();
            game.Assets.Add(new AssetItem("Overrides/Logo", new AssetObjectTest { Replaces = new UFile("/Plugin/DoesNotExist") }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestReplacementTypeMismatchFails()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            plugin.Assets.Add(new AssetItem("Logo", new AssetObjectTest()));
            game.Assets.Add(new AssetItem("Overrides/Logo", new AssetObjectTestSub { Replaces = new UFile("/Plugin/Logo") }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestReplacementOfSelfFails()
        {
            var (_, game, _) = CreateSessionWithPlugin();
            game.Assets.Add(new AssetItem("Logo", new AssetObjectTest { Replaces = new UFile("/MyGame/Logo") }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestReplacementOfSourceCodeAssetFails()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            plugin.Assets.Add(new AssetItem("Effect", new SourceCodeAssetTest()));
            game.Assets.Add(new AssetItem("Overrides/Effect", new SourceCodeAssetTest { Replaces = new UFile("/Plugin/Effect") }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestDerivedReplacementDoesNotSelfReference()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var target = new AssetItem("Logo", new AssetObjectTest { Name = "Original" });
            plugin.Assets.Add(target);

            // The editor's "Create replacing asset" derives from the target (archetype -> target)
            var derived = target.CreateDerivedAsset();
            derived.Replaces = new UFile("/Plugin/Logo");
            var replacement = new AssetItem("Overrides/Logo", derived);
            game.Assets.Add(replacement);

            var logger = new LoggerResult();
            Assert.True(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out var replacements));
            AssetReplacementAnalysis.Substitute(replacements);

            // The substituted clone carries the target's id: keeping the archetype would make it
            // reference itself
            var substituted = game.FindAsset(new UFile("/Plugin/Logo"));
            Assert.NotNull(substituted);
            Assert.Equal(target.Id, substituted.Id);
            Assert.Null(substituted.Asset.Archetype);
            // The authored replacer keeps its archetype (editor inheritance is untouched)
            Assert.NotNull(game.FindAsset(new UFile("/MyGame/Overrides/Logo")).Asset.Archetype);
        }

        [Fact]
        public void TestChainedReplacementFails()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var plugin2 = AddPlugin(game, "Plugin2");
            plugin.Assets.Add(new AssetItem("Logo", new AssetObjectTest()));
            plugin2.Assets.Add(new AssetItem("Fixups/Logo", new AssetObjectTest { Replaces = new UFile("/Plugin/Logo") }));
            game.Assets.Add(new AssetItem("Overrides/Logo", new AssetObjectTest { Replaces = new UFile("/Plugin2/Fixups/Logo") }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestRootPackageReplacementWinsOverDependency()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var plugin2 = AddPlugin(game, "Plugin2");
            plugin.Assets.Add(new AssetItem("Logo", new AssetObjectTest()));
            var gameReplacement = new AssetItem("Overrides/Logo", new AssetObjectTest { Replaces = new UFile("/Plugin/Logo") });
            game.Assets.Add(gameReplacement);
            plugin2.Assets.Add(new AssetItem("Fixups/Logo", new AssetObjectTest { Replaces = new UFile("/Plugin/Logo") }));

            var logger = new LoggerResult();
            Assert.True(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out var replacements));
            Assert.False(logger.HasErrors);
            Assert.Equal(gameReplacement.Id, Assert.Single(replacements).Replacement.Id);
        }

        [Fact]
        public void TestDuplicateReplacementSameScopeFails()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var plugin2 = AddPlugin(game, "Plugin2");
            var plugin3 = AddPlugin(game, "Plugin3");
            plugin.Assets.Add(new AssetItem("Logo", new AssetObjectTest()));
            plugin2.Assets.Add(new AssetItem("Fixups/Logo", new AssetObjectTest { Replaces = new UFile("/Plugin/Logo") }));
            plugin3.Assets.Add(new AssetItem("Fixups/Logo", new AssetObjectTest { Replaces = new UFile("/Plugin/Logo") }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }
    }

    [DataContract("!SourceCodeAssetTest")]
    [AssetDescription(".sdsrctest")]
    public class SourceCodeAssetTest : SourceCodeAsset
    {
    }
}
