// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
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
            var replacement = new AssetItem("Overrides/Logo", new AssetObjectTest { Name = "Replacement", Replaces = target.ToReference() });
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
        public void TestReplacementShipsWhenTargetIsRoot()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var target = new AssetItem("Logo", new AssetObjectTest { Name = "Original" });
            plugin.Assets.Add(target);
            var replacer = new AssetItem("Overrides/Logo", new AssetObjectTest { Name = "Replacement", Replaces = target.ToReference() });
            game.Assets.Add(replacer);

            // The target is a build root, so its content is substituted and shipped.
            game.RootAssets.Add(target.ToReference());

            var logger = new LoggerResult();
            Assert.True(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out var replacements));
            AssetReplacementAnalysis.Substitute(replacements);

            var included = new RootPackageAssetEnumerator(game).GetAssets(new AssetCompilerResult()).ToList();

            // The target slot ships with the replacement content...
            var shipped = Assert.Single(included, i => i.Id == target.Id);
            Assert.Equal("Replacement", ((AssetObjectTest)shipped.Asset).Name);
            // ...and the replacer is not compiled a second time (it is no longer force-rooted).
            Assert.DoesNotContain(included, i => i.Id == replacer.Id);
        }

        [Fact]
        public void TestReplacementDoesNotShipWhenTargetUnreachable()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var target = new AssetItem("Logo", new AssetObjectTest { Name = "Original" });
            plugin.Assets.Add(target);
            var replacer = new AssetItem("Overrides/Logo", new AssetObjectTest { Name = "Replacement", Replaces = target.ToReference() });
            game.Assets.Add(replacer);

            // No roots: the target is unreachable, so neither it nor the replacer ships (declaring
            // a replacement no longer force-roots the replacing asset).
            var logger = new LoggerResult();
            Assert.True(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out var replacements));
            AssetReplacementAnalysis.Substitute(replacements);

            var included = new RootPackageAssetEnumerator(game).GetAssets(new AssetCompilerResult()).ToList();

            Assert.DoesNotContain(included, i => i.Id == target.Id);
            Assert.DoesNotContain(included, i => i.Id == replacer.Id);
        }

        [Fact]
        public void TestReplacesIsNotABuildDependency()
        {
            var (session, game, plugin) = CreateSessionWithPlugin();
            var archetypeTarget = new AssetItem("Base", new AssetObjectTest());
            plugin.Assets.Add(archetypeTarget);
            var replaceTarget = new AssetItem("Logo", new AssetObjectTest());
            plugin.Assets.Add(replaceTarget);

            // The replacer derives from one asset (archetype) and replaces another.
            var derived = archetypeTarget.CreateDerivedAsset();
            derived.Replaces = replaceTarget.ToReference();
            var replacer = new AssetItem("Overrides/Logo", derived);
            game.Assets.Add(replacer);

            var deps = session.DependencyManager.ComputeDependencies(replacer.Id, AssetDependencySearchOptions.Out);
            Assert.NotNull(deps);
            // Archetype is a real dependency link (control: proves the graph is populated)...
            Assert.Contains(deps.LinksOut, l => l.Item.Id == archetypeTarget.Id);
            // ...but Replaces must NOT be a dependency (it is substituted at build time, not consumed).
            Assert.DoesNotContain(deps.LinksOut, l => l.Item.Id == replaceTarget.Id);
        }

        [Fact]
        public void TestReplacementMissingTargetFails()
        {
            var (_, game, _) = CreateSessionWithPlugin();
            // Neither the id nor the location resolves to an existing asset.
            game.Assets.Add(new AssetItem("Overrides/Logo", new AssetObjectTest { Replaces = new AssetReference(AssetId.New(), "/Plugin/DoesNotExist") }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestReplacementTypeMismatchFails()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var target = new AssetItem("Logo", new AssetObjectTest());
            plugin.Assets.Add(target);
            game.Assets.Add(new AssetItem("Overrides/Logo", new AssetObjectTestSub { Replaces = target.ToReference() }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestReplacementOfSelfFails()
        {
            var (_, game, _) = CreateSessionWithPlugin();
            var self = new AssetItem("Logo", new AssetObjectTest());
            game.Assets.Add(self);
            self.Asset.Replaces = self.ToReference();

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestReplacementOfSourceCodeAssetFails()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var target = new AssetItem("Effect", new SourceCodeAssetTest());
            plugin.Assets.Add(target);
            game.Assets.Add(new AssetItem("Overrides/Effect", new SourceCodeAssetTest { Replaces = target.ToReference() }));

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
            derived.Replaces = target.ToReference();
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
            var pluginLogo = new AssetItem("Logo", new AssetObjectTest());
            plugin.Assets.Add(pluginLogo);
            var plugin2Logo = new AssetItem("Fixups/Logo", new AssetObjectTest());
            plugin2.Assets.Add(plugin2Logo);
            plugin2Logo.Asset.Replaces = pluginLogo.ToReference();
            game.Assets.Add(new AssetItem("Overrides/Logo", new AssetObjectTest { Replaces = plugin2Logo.ToReference() }));

            var logger = new LoggerResult();
            Assert.False(AssetReplacementAnalysis.TryCollect(game, new HashSet<Package> { game }, logger, out _));
            Assert.True(logger.HasErrors);
        }

        [Fact]
        public void TestRootPackageReplacementWinsOverDependency()
        {
            var (_, game, plugin) = CreateSessionWithPlugin();
            var plugin2 = AddPlugin(game, "Plugin2");
            var pluginLogo = new AssetItem("Logo", new AssetObjectTest());
            plugin.Assets.Add(pluginLogo);
            var gameReplacement = new AssetItem("Overrides/Logo", new AssetObjectTest { Replaces = pluginLogo.ToReference() });
            game.Assets.Add(gameReplacement);
            plugin2.Assets.Add(new AssetItem("Fixups/Logo", new AssetObjectTest { Replaces = pluginLogo.ToReference() }));

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
            var pluginLogo = new AssetItem("Logo", new AssetObjectTest());
            plugin.Assets.Add(pluginLogo);
            plugin2.Assets.Add(new AssetItem("Fixups/Logo", new AssetObjectTest { Replaces = pluginLogo.ToReference() }));
            plugin3.Assets.Add(new AssetItem("Fixups/Logo", new AssetObjectTest { Replaces = pluginLogo.ToReference() }));

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
