// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.IO;
using System.Linq;
using Xunit;
using Stride.Assets.Effect;
using Stride.Assets.Skyboxes;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Tests that a skybox build reads the shaders through a <see cref="ShaderSourceLocator"/> instead of depending on every shader asset.
    /// </summary>
    public sealed class TestSkyboxAssetCompiler
    {
        private static readonly string Directory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Tests", nameof(TestSkyboxAssetCompiler));

        [Fact]
        public void TheSkyboxDoesNotDependOnTheShaderAssets()
        {
            var skybox = CreateSession("4.5.0", new SkyboxAsset());

            Assert.Empty(new SkyboxAssetCompiler().GetInputFiles(skybox));
        }

        [Fact]
        public void TheSkyboxBuildsAgainWithAnotherVersionOfAPackageWithShaders()
        {
            // The same skybox (its id is part of the hash) in the sessions of two versions of the engine
            var asset = new SkyboxAsset();
            var hash = ComputeCommandHash(CreateSession("4.5.0", asset), null);

            Assert.Equal(hash, ComputeCommandHash(CreateSession("4.5.0", asset), null));
            Assert.NotEqual(hash, ComputeCommandHash(CreateSession("4.5.1", asset), null));
        }

        [Fact]
        public void TheSkyboxUsesTheLocatorOfTheContext()
        {
            var skybox = CreateSession("4.5.0", new SkyboxAsset());

            // Game Studio gives the locator of its session; without one, the build creates it from the session
            var fromSession = ComputeCommandHash(skybox, null);
            Assert.NotEqual(fromSession, ComputeCommandHash(skybox, ShaderSourceLocator.Create([])));
            Assert.Equal(fromSession, ComputeCommandHash(skybox, ShaderSourceLocator.Create(skybox.Package!.Session!.Packages)));
        }

        private static AssetItem CreateSession(string engineVersion, SkyboxAsset asset)
        {
            var engine = new Package
            {
                FullPath = Path.Combine(Directory, "Engine", "Engine" + Package.PackageFileExtension),
                Meta = { Name = "Engine", Version = new PackageVersion(engineVersion) },
            };
            engine.Assets.Add(new AssetItem("Shaders/Lighting", new EffectShaderAsset()));

            var game = new Package { FullPath = Path.Combine(Directory, "Game", "Game" + Package.PackageFileExtension), Meta = { Name = "Game" } };
            var skybox = new AssetItem("Skybox", asset);
            game.Assets.Add(skybox);

            var session = new PackageSession();
            session.Projects.Add(new StandalonePackage(engine));
            session.Projects.Add(new StandalonePackage(game));
            return skybox;
        }

        private static ObjectId ComputeCommandHash(AssetItem skybox, ShaderSourceLocator? locator)
        {
            using var context = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };
            context.SetGameSettingsAsset(GameSettingsFactory.Create());
            if (locator is not null)
                context.Properties.Set(ShaderSourceLocator.Key, locator);

            var result = new SkyboxAssetCompiler().Prepare(context, skybox);
            Assert.False(result.HasErrors);
            Assert.NotNull(context.Properties.Get(ShaderSourceLocator.Key));

            var command = Assert.Single(result.BuildSteps!.Steps.OfType<CommandBuildStep>()).Command;
            using var stream = new MemoryStream();
            command.ComputeCommandHash(stream, new PrepareContext());
            return ObjectId.FromBytes(stream.ToArray());
        }

        private sealed class PrepareContext : IPrepareContext
        {
            public Logger Logger { get; } = GlobalLogger.GetLogger(nameof(TestSkyboxAssetCompiler));

            public ObjectId ComputeInputHash(UrlType type, string filePath) => ObjectId.Empty;
        }
    }
}
