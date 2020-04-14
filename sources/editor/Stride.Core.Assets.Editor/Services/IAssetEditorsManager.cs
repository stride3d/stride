// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// An interface representing a window that can open and close editor instances for assets.
    /// </summary>
    public interface IAssetEditorsManager
    {
        /// <summary>
        /// Immediately closes all open asset editor windows.
        /// </summary>
        /// <param name="save">True if files should be saved, false if files can be closed without asking user, null if user should be asked.</param>
        /// <returns>True if all windows succesfully closed, false if user interrupted (should only happen when <see cref="save"/> is null).</returns>
        bool CloseAllEditorWindows(bool? save);

        /// <summary>
        /// Immediately closes all editor windows that are hidden.
        /// </summary>
        void CloseAllHiddenWindows();

        /// <summary>
        /// Closes the curve editor window.
        /// </summary>
        void CloseCurveEditorWindow();

        /// <summary>
        /// Closes the editor window for the given asset. If no editor window for this asset exists, this method does nothing.
        /// </summary>
        /// <param name="asset">The asset for which to close the editor window.</param>
        /// <param name="save">True if file should be saved, false if file can be closed without asking user, null if user should be asked.</param>
        /// <returns>True if windows could be closed, false if user interrupted (should only happen when <see cref="save"/> is null).</returns>
        bool CloseAssetEditorWindow(AssetViewModel asset, bool? save);

        /// <summary>
        /// Immediately hides all editor windows.
        /// </summary>
        void HideAllAssetEditorWindows();

        /// <summary>
        /// Opens (and activates) an editor window for the given <paramref name="curve"/>.
        /// </summary>
        /// <remarks>
        /// If the curve editor is already opened, adds the curve to the editor and activates it.
        /// </remarks>
        /// <param name="curve">The curve to show in the curve editor.</param>
        /// <param name="name">The name to display in the curve editor.</param>
        void OpenCurveEditorWindow(object curve, string name);

        /// <summary>
        /// Opens (and activates) an editor window for the given asset. If an editor window for this asset already exists, simply activates it.
        /// </summary>
        /// <param name="asset">The asset for which to show an editor window.</param>
        Task OpenAssetEditorWindow(AssetViewModel asset);
    }
}
