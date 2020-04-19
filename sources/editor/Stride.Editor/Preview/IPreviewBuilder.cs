// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Editor.Build;

namespace Stride.Editor.Preview
{
    /// <summary>
    /// An interface that represents an object which is capable of building previews for assets.
    /// </summary>
    public interface IPreviewBuilder
    {
        /// <summary>
        /// Gets the asset builder service used to build asset.
        /// </summary>
        GameStudioBuilderService AssetBuilderService { get; }

        /// <summary>
        /// Gets the <see cref="IDispatcherService"/> to use to update UI.
        /// </summary>
        IDispatcherService Dispatcher { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/> to use for preview logs.
        /// </summary>
        Logger Logger { get; }

        /// <summary>
        /// Gets the instance of <see cref="Preview.PreviewGame"/> to use for preview.
        /// </summary>
        PreviewGame PreviewGame { get; }

        /// <summary>
        /// Compiles the given asset (and its dependencies).
        /// </summary>
        /// <param name="asset">The asset to compile.</param>
        /// <returns>An <see cref="AssetCompilerResult"/> containing the generated build steps.</returns>
        AssetCompilerResult Compile(AssetItem asset);

        /// <summary>
        /// Gets the framework element that contains the stride viewport.
        /// </summary>
        /// <returns></returns>
        FrameworkElement GetStrideView();
    }
}
