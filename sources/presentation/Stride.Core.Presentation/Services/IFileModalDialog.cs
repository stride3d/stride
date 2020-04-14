// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Presentation.Services
{
    /// <summary>
    /// An interface representing modal file dialogs.
    /// </summary>
    public interface IFileModalDialog : IModalDialog
    {
        /// <summary>
        /// Gets or sets the list of filter to use in the file dialog.
        /// </summary>
        IList<FileDialogFilter> Filters { get; set; }

        /// <summary>
        /// Gets or sets the initial directory of the file dialog.
        /// </summary>
        string InitialDirectory { get; set; }

        /// <summary>
        /// Gets or sets the default file name to display when opening the file dialog.
        /// </summary>
        string DefaultFileName { get; set; }
    }
}
