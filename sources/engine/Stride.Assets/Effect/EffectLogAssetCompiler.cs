// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;

namespace Stride.Assets.Effect
{
    /// <summary>
    /// Compiles same effects as a previous recorded session.
    /// </summary>
    [AssetCompiler(typeof(EffectLogAsset), typeof(AssetCompilationContext))]
    public class EffectLogAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            foreach (var sessionPackage in assetItem.Package.Session.Packages)
            {
                foreach (var sessionPackageAsset in sessionPackage.Assets)
                {
                    if (sessionPackageAsset.Asset is EffectShaderAsset)
                    {
                        yield return new ObjectUrl(UrlType.Content, sessionPackageAsset.Location);
                    }
                }
            }
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var originalSourcePath = assetItem.FullPath;

            result.BuildSteps = new AssetBuildStep(assetItem);

            var urlRoot = originalSourcePath.GetParent();

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(((EffectLogAsset)assetItem.Asset).Text));
            using (var recordedEffectCompile = new EffectLogStore(stream))
            {
                recordedEffectCompile.LoadNewValues();

                foreach (var entry in recordedEffectCompile.GetValues())
                {
                    result.BuildSteps.Add(EffectCompileCommand.FromRequest(context, assetItem.Package, urlRoot, entry.Key));
                }
            }
        }
    }
}
