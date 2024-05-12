// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// This interface represents the view of an asset editor.
    /// </summary>
    public interface IEditorView
    {
        /// <summary>
        /// Gets or sets the data context for an element when it participates in data binding.
        /// </summary>
        object DataContext { get; set; }

        /// <summary>
        /// Gets a task that is completed when the initialization is done.
        /// </summary>
        [NotNull]
        Task EditorInitialization { get; }

        /// <summary>
        /// Initializes the editor view with the editor.
        /// </summary>
        /// <param name="editor">The editor for which to initialize the editor view.</param>
        /// <returns>A task that completes when the initialization is done.</returns>
        Task<bool> InitializeEditor([NotNull] IAssetEditorViewModel editor);
    }
}
