// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;
using Stride.Editor.Engine;
using Stride.UI.Editor.Services;
using Stride.UI.Editor.ViewModels;

namespace Stride.UI.Editor.Services
{
    /// <summary>
    /// Game controller for the UI library editor.
    /// </summary>
    public sealed class UILibraryEditorController : UIEditorController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIEditorController"/> class.
        /// </summary>
        /// <param name="editor">The editor associated with this instance.</param>
        /// <param name="viewportService">The viewport service of the session, which creates the viewport of the game.</param>
        public UILibraryEditorController([NotNull] UILibraryEditorViewModel editor, [NotNull] IEditorGameViewportService viewportService)
            : base(editor, viewportService)
        {
        }
    }
}
