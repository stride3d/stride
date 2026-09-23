// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Effect;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
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
                // Recompile shaders. The build service is registered during plugin initialization, so it is
                // not available yet when a save runs while opening the session (e.g. persisting an upgrade);
                // skipping is fine there since the service compiles shaders from all packages when it starts.
                var builder = Session.ServiceProvider.TryGet<AssetBuilderService>();
                if (builder == null)
                    return;

                var shaderImporter = new StrideShaderImporter();
                var shaderBuildSteps = shaderImporter.CreateUserShaderBuildSteps(Session);
                builder.PushBuildUnit(new PrecompiledAssetBuildUnit(AssetBuildUnitIdentifier.Default, shaderBuildSteps, true));
            }
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
