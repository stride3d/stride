// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;
using Stride.TextureConverter;
using Stride.Graphics;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont
{
    [AssetCompiler(typeof(PrecompiledSpriteFontAsset), typeof(AssetCompilationContext))]
    public class PrecompiledSpriteFontAssetCompiler : AssetCompilerBase
    {
        private static readonly FontDataFactory FontDataFactory = new FontDataFactory();

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (PrecompiledSpriteFontAsset)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new PrecompiledSpriteFontCommand(targetUrlInStorage, asset, assetItem.Package));
        }

        internal class PrecompiledSpriteFontCommand : AssetCommand<PrecompiledSpriteFontAsset>
        {
            public PrecompiledSpriteFontCommand(string url, PrecompiledSpriteFontAsset description, IAssetFinder assetFinder)
                : base(url, description, assetFinder)
            {
            }

            public override IEnumerable<ObjectUrl> GetInputFiles()
            {
                yield return new ObjectUrl(UrlType.File, Parameters.FontDataFile);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                using (var texTool = new TextureTool())
                using (var texImage = texTool.Load(Parameters.FontDataFile, Parameters.IsSrgb))
                {
                    //make sure we are RGBA and not BGRA
                    texTool.Convert(texImage, Parameters.IsSrgb ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm);

                    var image = texTool.ConvertToStrideImage(texImage);

                    Graphics.SpriteFont staticFont = FontDataFactory.NewStatic(
                        Parameters.Size,
                        Parameters.Glyphs,
                        new[] { image },
                        Parameters.BaseOffset,
                        Parameters.DefaultLineSpacing,
                        Parameters.Kernings,
                        Parameters.ExtraSpacing,
                        Parameters.ExtraLineSpacing,
                        Parameters.DefaultCharacter);

                    // save the data into the database
                    var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                    assetManager.Save(Url, staticFont);

                    image.Dispose();
                }

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
