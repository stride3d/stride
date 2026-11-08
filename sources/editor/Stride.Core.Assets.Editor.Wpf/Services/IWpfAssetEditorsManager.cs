// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// The asset editors manager of the WPF editor: the shared <see cref="IAssetEditorsManager"/>, and what only the WPF docking
    /// windows do (the curve editor window, hidden windows, synchronous closing).
    /// </summary>
    public interface IWpfAssetEditorsManager : IAssetEditorsManager
    {
        /// <summary>
        /// Closes all editor windows.
        /// </summary>
        /// <param name="save">The save behavior: <c>true</c> to save, <c>false</c> to drop the changes, <c>null</c> to ask.</param>
        /// <returns><c>true</c> if all windows were closed, <c>false</c> otherwise.</returns>
        bool CloseAllEditorWindows(bool? save);

        /// <summary>
        /// Closes all hidden windows.
        /// </summary>
        void CloseAllHiddenWindows();

        /// <summary>
        /// Closes the curve editor window.
        /// </summary>
        void CloseCurveEditorWindow();

        /// <summary>
        /// Closes the editor window of the given asset.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="save">The save behavior: <c>true</c> to save, <c>false</c> to drop the changes, <c>null</c> to ask.</param>
        /// <returns><c>true</c> if the editor window was closed or the asset had none, <c>false</c> otherwise.</returns>
        bool CloseAssetEditorWindow(AssetViewModel asset, bool? save);

        /// <summary>
        /// Hides all editor windows.
        /// </summary>
        void HideAllAssetEditorWindows();

        /// <summary>
        /// Opens (and activates) the curve editor window for the given curve.
        /// </summary>
        void OpenCurveEditorWindow(object curve, string name);
    }
}
