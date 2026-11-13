// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Effect;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;

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
        protected override void Initialize()
        {
            base.Initialize();

            // Keeps the text of a project file in memory: an undo writes it back if the file is deleted from outside
            if (Asset is IProjectAsset && !Directory.Package.Package.IsReadOnly)
                _ = Asset.Text;
        }

        /// <inheritdoc/>
        protected override void OnSessionSaved()
        {
            if (Asset is EffectShaderAsset)
                StrideAssetsViewModel.Instance?.Code.RequestShaderSourcesUpdate((SessionViewModel)Session);
        }
        
        /// <summary>
        /// The full path to the source code asset on disk
        /// </summary>
        public UFile FullPath => AssetItem.FullPath;
    }

    [AssetViewModel<SourceCodeAsset>]
    public class CodeAssetViewModel : CodeAssetViewModel<SourceCodeAsset>
    {
        public CodeAssetViewModel(AssetViewModelConstructionParameters parameters) : base(parameters)
        {

        }
    }
}
