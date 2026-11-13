// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Assets.Effect;
using Stride.Assets.Textures;
using Stride.Graphics;

namespace Stride.Assets.Skyboxes
{
    [AssetCompiler(typeof(SkyboxAsset), typeof(AssetCompilationContext))]
    internal class SkyboxAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(TextureAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SkyboxAsset)assetItem.Asset;

            var colorSpace = context.GetColorSpace();

            result.BuildSteps = new AssetBuildStep(assetItem);

            var prereqs = new Queue<BuildStep>();

            // build the textures for windows (needed for skybox compilation)
            foreach (var dependency in asset.GetDependencies())
            {
                var dependencyItem = assetItem.Package.Assets.Find(dependency.Id);
                if (dependencyItem?.Asset is TextureAsset)
                {
                    var textureAsset = (TextureAsset)dependencyItem.Asset;

                    // Get absolute path of asset source on disk
                    var assetSource = GetAbsolutePath(dependencyItem, textureAsset.Source);

                    // Create a synthetic url
                    var textureUrl = SkyboxGenerator.BuildTextureForSkyboxGenerationLocation(dependencyItem.Location);

                    var gameSettingsAsset = context.GetGameSettingsAsset();
                    var renderingSettings = gameSettingsAsset.GetOrCreate<RenderingSettings>();

                    // Select the best graphics profile
                    var graphicsProfile = renderingSettings.DefaultGraphicsProfile >= GraphicsProfile.Level_10_0 ? renderingSettings.DefaultGraphicsProfile : GraphicsProfile.Level_10_0;

                    var textureAssetItem = new AssetItem(textureUrl, textureAsset);

                    // Create and add the texture command.
                    var textureParameters = new TextureConvertParameters(assetSource, textureAsset, context.Platform, context.Platform.GetDefaultGraphicsPlatform(), graphicsProfile, gameSettingsAsset.GetOrCreate<TextureSettings>().TextureQuality, colorSpace);
                    var prereqStep = new AssetBuildStep(textureAssetItem);
                    prereqStep.Add(new TextureAssetCompiler.TextureConvertCommand(textureUrl, textureParameters, assetItem.Package));
                    result.BuildSteps.Add(prereqStep);
                    prereqs.Enqueue(prereqStep);
                }
            }

            // add the skybox command itself.
            IEnumerable<ObjectUrl> InputFilesGetter()
            {
                var skyboxAsset = (SkyboxAsset)assetItem.Asset;
                foreach (var dependency in skyboxAsset.GetDependencies())
                {
                    var dependencyItem = assetItem.Package.Assets.Find(dependency.Id);
                    if (dependencyItem?.Asset is TextureAsset)
                    {
                        yield return new ObjectUrl(UrlType.Content, dependency.Location);
                    }
                }
            }

            // The effects read the shader files where they are: the command registers the ones it used, instead of depending on every shader asset
            var shaderSources = ShaderSourceLocator.GetOrCreate(context, assetItem.Package.Session);
            var assetStep = new CommandBuildStep(new SkyboxCompileCommand(targetUrlInStorage, asset, assetItem.Package, shaderSources)
            {
                InputFilesGetter = InputFilesGetter
            });
            result.BuildSteps.Add(assetStep);
            while (prereqs.Count > 0)
            {
                var prereq = prereqs.Dequeue();
                BuildStep.LinkBuildSteps(prereq, assetStep);
            }
        }

        internal class SkyboxCompileCommand : AssetCommand<SkyboxAsset>
        {
            private readonly ShaderSourceLocator shaderSources;

            public SkyboxCompileCommand(string url, SkyboxAsset parameters, IAssetFinder assetFinder, ShaderSourceLocator shaderSources)
                : base(url, parameters, assetFinder)
            {
                // 3: the results register the shader files they used; older ones would match whatever the shaders became
                Version = 3;
                this.shaderSources = shaderSources;
            }

            /// <inheritdoc/>
            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);
                writer.Write(2); // Change this number to recompute the hash when prefiltering algorithm are changed

                // Another version of a package brings its shaders in other files, while the files of the last build are often still there, unchanged
                foreach (var (name, version) in shaderSources.Packages)
                {
                    writer.Write(name);
                    writer.Write(version ?? string.Empty);
                }
            }

            /// <inheritdoc/>
            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // TODO Convert SkyboxAsset to Skybox and save to Skybox object
                // TODO Add system to prefilter

                using var shaderFiles = new ShaderSourceFileProvider(shaderSources, MicrothreadLocalDatabases.DatabaseFileProvider);
                using (var context = new SkyboxGeneratorContext(Parameters, MicrothreadLocalDatabases.ProviderService, shaderFiles))
                {
                    var result = SkyboxGenerator.Compile(Parameters, context);

                    // A change of a shader the effects used builds the skybox again; the effects check their cached bytecode against
                    // the content of each of their shaders, so a cache hit reads them too
                    foreach (var filePath in shaderFiles.ReadSources)
                        commandContext.RegisterInputDependency(new ObjectUrl(UrlType.File, filePath));

                    if (result.HasErrors)
                    {
                        result.CopyTo(commandContext.Logger);
                        return Task.FromResult(ResultStatus.Failed);
                    }

                    context.Content.Save(Url, result.Skybox);
                }

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
 
