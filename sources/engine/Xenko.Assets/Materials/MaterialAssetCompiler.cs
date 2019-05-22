// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Assets.Textures;
using Xenko.Graphics;
using Xenko.Rendering.Materials;

namespace Xenko.Assets.Materials
{
    [AssetCompiler(typeof(MaterialAsset), typeof(AssetCompilationContext))]
    internal class MaterialAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(TextureAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
            yield return new BuildDependencyInfo(typeof(MaterialAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileAsset);
            yield return new BuildDependencyInfo(typeof(GameSettingsAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileAsset);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            // Note: might not be needed in all cases, but let's not bother for now (they are only 9kb)
            yield return new ObjectUrl(UrlType.Content, "XenkoEnvironmentLightingDFGLUT16");
            yield return new ObjectUrl(UrlType.Content, "XenkoEnvironmentLightingDFGLUT8");
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (MaterialAsset)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new MaterialCompileCommand(targetUrlInStorage, assetItem, asset, context));
        }

        private class MaterialCompileCommand : AssetCommand<MaterialAsset>
        {
            private readonly AssetItem assetItem;

            private readonly GraphicsProfile graphicsProfile;
            private readonly ColorSpace colorSpace;

            private UFile assetUrl;

            public MaterialCompileCommand(string url, AssetItem assetItem, MaterialAsset value, AssetCompilerContext context)
                : base(url, value, assetItem.Package)
            {
                Version = 4;
                this.assetItem = assetItem;
                colorSpace = context.GetColorSpace();
                assetUrl = new UFile(url);

                graphicsProfile = context.GetGameSettingsAsset().GetOrCreate<RenderingSettings>(context.Platform).DefaultGraphicsProfile;
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);

                writer.Serialize(ref assetUrl, ArchiveMode.Serialize);

                // Write graphics profile and color space
                writer.Write(graphicsProfile);
                writer.Write(colorSpace);

                foreach (var compileTimeDependency in ((MaterialAsset)assetItem.Asset).FindMaterialReferences())
                {
                    var linkedAsset = AssetFinder.FindAsset(compileTimeDependency.Id);
                    if (linkedAsset?.Asset != null)
                    {
                        writer.SerializeExtended(linkedAsset.Asset, ArchiveMode.Serialize);
                    }
                }
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // Reduce trees on CPU
                //var materialReducer = new MaterialTreeReducer(material);
                //materialReducer.ReduceTrees();

                //foreach (var reducedTree in materialReducer.ReducedTrees)
                //{
                //    material.Nodes[reducedTree.Key] = reducedTree.Value;
                //}

                // Reduce on GPU 
                // TODO: Adapt GPU reduction so that it is compatible Android color/alpha separation
                // TODO: Use the build engine processed output textures instead of the imported one (not existing any more)
                // TODO: Set the reduced texture output format
                // TODO: The graphics device cannot be shared with the Previewer
                //var graphicsDevice = (GraphicsDevice)context.Attributes.GetOrAdd(CompilerContext.GraphicsDeviceKey, key => GraphicsDevice.New(DeviceCreationFlags.None, GraphicsProfile.Level_11_0));
                //using (var materialTextureLayerFlattener = new MaterialTextureLayerFlattener(material, graphicsDevice))
                //{
                //    materialTextureLayerFlattener.PrepareForFlattening(new UDirectory(assetUrl.Directory));
                //    if (materialTextureLayerFlattener.HasCommands)
                //    {
                //        var compiler = EffectCompileCommand.GetOrCreateEffectCompiler(context);
                //        materialTextureLayerFlattener.Run(compiler);
                //        // store Material with modified textures
                //        material = materialTextureLayerFlattener.Material;
                //    }
                //}

                // Check with Ben why DoCommandOverride is called without going through the constructor?
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                var materialContext = new MaterialGeneratorContext
                {
                    GraphicsProfile = graphicsProfile,
                    Content = assetManager,
                    ColorSpace = colorSpace
                };
                materialContext.AddLoadingFromSession(AssetFinder);

                var materialClone = AssetCloner.Clone(Parameters);
                var result = MaterialGenerator.Generate(new MaterialDescriptor { MaterialId = materialClone.Id, Attributes = materialClone.Attributes, Layers = materialClone.Layers}, materialContext, string.Format("{0}:{1}", materialClone.Id, assetUrl));

                if (result.HasErrors)
                {
                    result.CopyTo(commandContext.Logger);
                    return Task.FromResult(ResultStatus.Failed);
                }
                // Separate the textures into color/alpha components on Android to be able to use native ETC1 compression
                //if (context.Platform == PlatformType.Android)
                //{
                //    var alphaComponentSplitter = new TextureAlphaComponentSplitter(assetItem.Package.Session);
                //    material = alphaComponentSplitter.Run(material, new UDirectory(assetUrl.GetDirectory())); // store Material with alpha substituted textures
                //}

                // Create the parameters
                //var materialParameterCreator = new MaterialParametersCreator(material, assetUrl);
                //if (materialParameterCreator.CreateParameterCollectionData(commandContext.Logger))
                //    return Task.FromResult(ResultStatus.Failed);

                assetManager.Save(assetUrl, result.Material);

                return Task.FromResult(ResultStatus.Successful);
            }
            
            public override string ToString()
            {
                return (assetUrl ?? "[File]") + " (Material) > " + (assetUrl ?? "[Location]");
            }
        }
    }
}
 
