// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Presentation.Dirtiables;
using Stride.Assets.Effect;
using Stride.Editor.Build;

namespace Stride.Assets.Presentation.ViewModel
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
                var shaderImporter = new StrideShaderImporter();
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
