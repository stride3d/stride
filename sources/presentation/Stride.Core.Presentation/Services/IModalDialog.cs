// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

namespace Stride.Core.Presentation.Services
{
    /// <summary>
    /// An interface representing a modal dialog.
    /// </summary>
    public interface IModalDialog
    {
        /// <summary>
        /// Displays the modal dialog. This method returns a task that completes when the user close the dialog.
        /// </summary>
        /// <returns>A <see cref="DialogResult"/> value indicating how the user closed the dialog.</returns>
        Task<DialogResult> ShowModal();

        /// <summary>
        /// Requests this dialog to close. This method returns immediately and does not wait for the dialog to be closed.
        /// </summary>
        /// <param name="result"></param>
        /// <remarks>This method does not guarantee that the dialog will actually close, a cancel can still occurs.</remarks>
        void RequestClose(DialogResult result);

        /// <summary>
        /// Gets or sets a data context for the modal dialog.
        /// </summary>
        object DataContext { get; set; }
    }
}
