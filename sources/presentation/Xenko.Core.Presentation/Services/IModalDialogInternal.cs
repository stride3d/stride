// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Presentation.Services
{
    /// <summary>
    /// An internal interface representing a modal dialog.
    /// </summary>
    public interface IModalDialogInternal : IModalDialog
    {
        /// <summary>
        /// Gets or sets the result of the modal dialog.
        /// </summary>
        DialogResult Result { get; set; }
    }
}
