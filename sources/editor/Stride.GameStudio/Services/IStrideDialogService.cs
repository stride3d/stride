// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Services;

namespace Stride.GameStudio.Services
{
    /// <summary>
    /// This interface represents the dialog service used for the GameStudio. It extends <see cref="IEditorDialogService"/> with some GameStudio-specific dialogs.
    /// </summary>
    public interface IStrideDialogService : IEditorDialogService
    {
        /// <summary>
        /// Creates and displays the credentials dialog
        /// </summary>
        /// <returns>An instace of the <see cref="ICredentialsDialog"/> interface.</returns>
        ICredentialsDialog CreateCredentialsDialog();

        void ShowAboutPage();

    }
}
