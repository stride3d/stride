// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Editor.Engine;
using Stride.UI.Editor.Services;
using Stride.UI.Editor.ViewModels;
using Stride.UI.Assets;
using Stride.UI;

namespace Stride.UI.Editor.Services
{
    /// <summary>
    /// Game controller for the UI page editor.
    /// </summary>
    public sealed class UIPageEditorController : UIEditorController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIEditorController"/> class.
        /// </summary>
        /// <param name="editor">The editor associated with this instance.</param>
        /// <param name="viewportService">The viewport service of the session, which creates the viewport of the game.</param>
        public UIPageEditorController([NotNull] UIPageEditorViewModel editor, [NotNull] IEditorGameViewportService viewportService)
            : base(editor, viewportService)
        {
        }

        /// <inheritdoc/>
        protected override bool ConstructRootElements(out ICollection<UIElement> rootElements, out UIAssetBase.UIDesign editorSettings)
        {
            var uiAsset = (UIAssetBase)Asset.Asset;
            if (uiAsset.Hierarchy.RootParts.Count > 1)
            {
                Editor.Logger.Error(UIPageRootViewModel.OneRootOnly);
                rootElements = null;
                editorSettings = null;
                return false;
            }

            return base.ConstructRootElements(out rootElements, out editorSettings);
        }
    }
}
