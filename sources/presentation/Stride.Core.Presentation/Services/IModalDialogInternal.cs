// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Presentation.Services
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
