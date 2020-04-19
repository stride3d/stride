// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Rendering.RenderTextures;

namespace Stride.Assets.Textures
{
    [AssetCompiler(typeof(RenderTextureAsset), typeof(AssetCompilationContext))]
    public class RenderTextureAssetCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (RenderTextureAsset)assetItem.Asset;
            var colorSpace = context.GetColorSpace();

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new RenderTextureConvertCommand(targetUrlInStorage, new RenderTextureParameters(asset, colorSpace), assetItem.Package));
        }

        /// <summary>
        /// Command used to convert the texture in the storage
        /// </summary>
        private class RenderTextureConvertCommand : AssetCommand<RenderTextureParameters>
        {
            public RenderTextureConvertCommand(string url, RenderTextureParameters parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                assetManager.Save(Url, new RenderTextureDescriptor
                {
                    Width = Parameters.Asset.Width,
                    Height = Parameters.Asset.Height,
                    Format = Parameters.Asset.Format,
                    ColorSpace = Parameters.Asset.IsSRgb(Parameters.ColorSpace) ? ColorSpace.Linear : ColorSpace.Gamma,
                });

                return Task.FromResult(ResultStatus.Successful);
            }
        }

        [DataContract]
        public struct RenderTextureParameters
        {
            public RenderTextureAsset Asset;
            public ColorSpace ColorSpace;

            public RenderTextureParameters(RenderTextureAsset asset, ColorSpace colorSpace)
            {
                Asset = asset;
                ColorSpace = colorSpace;
            }
        }
    }
}
