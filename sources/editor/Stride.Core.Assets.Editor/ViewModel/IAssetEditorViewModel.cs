// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// An interface that represents the view model of an asset editor.
    /// </summary>
    public interface IAssetEditorViewModel : IDestroyable
    {
        /// <summary>
        /// The asset related to this editor.
        /// </summary>
        [NotNull]
        [Obsolete("This property will be removed soon")]
        AssetViewModel Asset { get; }

        /// <summary>
        /// Initializes this editor asynchronously.
        /// </summary>
        /// <returns>A task that completes when the editor initialization has ended. This task result indicates if the initialization was successful.</returns>
        Task<bool> Initialize();

        /// <summary>
        /// Processes anything before closing the editor, such as saving the file.
        /// </summary>
        /// <param name="save"><c>true</c> if file should be saved, <c>false</c> if the file can be closed without asking user, null if user should be asked.</param>
        /// <returns><c>true</c> if windows can be closed, <c>false</c> if user interrupted (should only happen when <see cref="save"/> is null).</returns>
        bool PreviewClose(bool? save);
    }
}
