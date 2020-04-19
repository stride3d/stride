// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Presentation.AssetEditors.AssetHighlighters
{
    /// <summary>
    /// The base class to implement asset highlighter. An asset highlighter is an object that is capable of highlighting the given asset in the viewport of
    /// the scene editor, and also clear all assets it has previously highlighted.
    /// </summary>
    public abstract class AssetHighlighter
    {
        /// <summary>
        /// The dependency manager of the current session.
        /// </summary>
        protected readonly IAssetDependencyManager DependencyManager;

        static AssetHighlighter()
        {
            DirectReferenceColor = new Color4(1.0f, 0.35f, 0.25f, 0.8f);
            IndirectReferenceColor = new Color4(1.0f, 0.65f, 0.60f, 0.8f);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetHighlighter"/> class.
        /// </summary>
        /// <param name="dependencyManager">The dependency manager of the current session.</param>
        protected AssetHighlighter([NotNull] IAssetDependencyManager dependencyManager)
        {
            if (dependencyManager == null) throw new ArgumentNullException(nameof(dependencyManager));
            DependencyManager = dependencyManager;
        }

        /// <summary>
        /// Gets the color to use to highlight directly referenced assets.
        /// </summary>
        /// <remarks>This color does not have premultiplied alpha.</remarks>
        public static Color4 DirectReferenceColor { get; private set; }

        /// <summary>
        /// Gets the color to use to highlight indirectly referenced assets.
        /// </summary>
        /// <remarks>This color does not have premultiplied alpha.</remarks>
        public static Color4 IndirectReferenceColor { get; private set; }

        /// <summary>
        /// Highlights the given asset in the scene viewport.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="game"></param>
        /// <param name="assetItem"></param>
        /// <param name="duration"></param>
        /// <remarks>This method is executed from the scene game thread.</remarks>
        public abstract void Highlight(IEditorGameController controller, EditorServiceGame game, AssetItem assetItem, float duration);

        /// <summary>
        /// Clear any asset highlighted by this instance in the scene viewport.
        /// </summary>
        /// <remarks>This method is executed from the scene game thread.</remarks>
        public abstract void Clear();
    }
}
