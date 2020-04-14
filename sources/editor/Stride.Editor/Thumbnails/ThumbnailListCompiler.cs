// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Assets;
using Stride.Editor.Preview;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// A thumbnail list compiler.
    /// This compiler creates the list of build steps to perform to produce the thumbnails of an list of <see cref="AssetItem"/>.
    /// </summary>
    public class ThumbnailListCompiler : ItemListCompiler
    {
        private readonly ThumbnailGenerator generator;
        private readonly EventHandler<ThumbnailBuiltEventArgs> builtAction;
        private readonly AssetDependenciesCompiler dependenciesCompiler = new AssetDependenciesCompiler(typeof(PreviewCompilationContext));

        /// <summary>
        /// Creates an instance of <see cref="ThumbnailListCompiler"/>.
        /// </summary>
        public ThumbnailListCompiler(ThumbnailGenerator generator, EventHandler<ThumbnailBuiltEventArgs> builtAction, AssetCompilerRegistry thumbnailCompilerRegistry)
            : base(thumbnailCompilerRegistry, typeof(ThumbnailCompilationContext))
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            if (thumbnailCompilerRegistry == null) throw new ArgumentNullException(nameof(thumbnailCompilerRegistry));

            this.generator = generator;
            this.builtAction = builtAction;
        }

        /// <summary>
        /// Generates a <see cref="ListBuildStep"/> to compile the thumbnail of the given asset.
        /// </summary>
        /// <param name="assetItem">The asset to compile.</param>
        /// <param name="gameSettings">The current game settings</param>
        /// <param name="staticThumbnail">If the asset has to be compiled as well in the case of non static thumbnail</param>
        /// <returns>A <see cref="ListBuildStep"/> containing the build steps to generate the thumbnail of the given asset.</returns>
        public ListBuildStep Compile(AssetItem assetItem, GameSettingsAsset gameSettings, bool staticThumbnail)
        {
            if (assetItem == null) throw new ArgumentNullException(nameof(assetItem));

            using (var context = new ThumbnailCompilerContext
            {
                Platform = PlatformType.Windows
            })
            {
                context.SetGameSettingsAsset(gameSettings);
                context.CompilationContext = typeof(PreviewCompilationContext);

                context.Properties.Set(ThumbnailGenerator.Key, generator);
                context.ThumbnailBuilt += builtAction;

                var result = new AssetCompilerResult();

                if (!staticThumbnail)
                {
                    //compile the actual asset                  
                    result = dependenciesCompiler.Prepare(context, assetItem);
                }

                //compile the actual thumbnail
                var thumbnailStep = CompileItem(context, result, assetItem);

                foreach (var buildStep in result.BuildSteps)
                {
                    BuildStep.LinkBuildSteps(buildStep, thumbnailStep);
                }
                result.BuildSteps.Add(thumbnailStep);
                return result.BuildSteps;
            }
        }
    }
}
