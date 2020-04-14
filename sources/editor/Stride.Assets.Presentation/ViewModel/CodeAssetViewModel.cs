// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Dirtiables;
using Xenko.Assets.Effect;
using Xenko.Editor.Build;

namespace Xenko.Assets.Presentation.ViewModel
{
    /// <summary>
    /// View model for <see cref="SourceCodeAsset"/>.
    /// </summary>
    /// <typeparam name="TSourceCodeAsset"></typeparam>
    public abstract class CodeAssetViewModel<TSourceCodeAsset> : AssetViewModel<TSourceCodeAsset> where TSourceCodeAsset : SourceCodeAsset
    {
        protected CodeAssetViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }

        /// <inheritdoc/>
        protected override void OnSessionSaved()
        {
            if (Asset is EffectShaderAsset)
            {
                //recompile shaders...
                var shaderImporter = new XenkoShaderImporter();
                var shaderBuildSteps = shaderImporter.CreateUserShaderBuildSteps(Session);
                var builder = Session.ServiceProvider.Get<AssetBuilderService>();
                builder.PushBuildUnit(new PrecompiledAssetBuildUnit(AssetBuildUnitIdentifier.Default, shaderBuildSteps, true));
            }
        }
        
        /// <summary>
        /// The full path to the source code asset on disk
        /// </summary>
        public UFile FullPath => AssetItem.FullPath;
    }

    [AssetViewModel(typeof(SourceCodeAsset))]
    public class CodeAssetViewModel : CodeAssetViewModel<SourceCodeAsset>
    {
        public CodeAssetViewModel(AssetViewModelConstructionParameters parameters) : base(parameters)
        {

        }
    }
}
