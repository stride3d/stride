// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core.Serialization.Contents;
using Xenko.Assets.Textures;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Rendering
{
    [AssetCompiler(typeof(GraphicsCompositorAsset), typeof(AssetCompilationContext))]
    public class GraphicsCompositorAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(RenderTextureAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileAsset);
            yield return new BuildDependencyInfo(typeof(TextureAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileAsset);
        }

        public override bool AlwaysCheckRuntimeTypes { get; } = true; //compositor is special, we always want to visit what the renderers

        public override IEnumerable<Type> GetRuntimeTypes(AssetItem assetItem)
        {
            yield return typeof(RendererCoreBase);
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (GraphicsCompositorAsset)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new GraphicsCompositorCompileCommand(targetUrlInStorage, asset, assetItem.Package));
        }

        internal class GraphicsCompositorCompileCommand : AssetCommand<GraphicsCompositorAsset>
        {
            public GraphicsCompositorCompileCommand(string url, GraphicsCompositorAsset asset, IAssetFinder assetFinder) : base(url, asset, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var graphicsCompositor = new GraphicsCompositor();

                foreach (var cameraSlot in Parameters.Cameras)
                    graphicsCompositor.Cameras.Add(cameraSlot);
                foreach (var renderStage in Parameters.RenderStages)
                    graphicsCompositor.RenderStages.Add(renderStage);
                foreach (var renderFeature in Parameters.RenderFeatures)
                    graphicsCompositor.RenderFeatures.Add(renderFeature);
                graphicsCompositor.Game = Parameters.Game;
                graphicsCompositor.SingleView = Parameters.SingleView;
                graphicsCompositor.Editor = Parameters.Editor;

                var assetManager = new ContentManager();
                assetManager.Save(Url, graphicsCompositor);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
