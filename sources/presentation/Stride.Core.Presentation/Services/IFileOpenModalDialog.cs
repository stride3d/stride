// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Presentation.Services
{
    /// <summary>
    /// An interface representing a modal file open dialog.
    /// </summary>
    public interface IFileOpenModalDialog : IFileModalDialog
    {
        /// <summary>
        /// Gets or sets whether multi-selection is allowed.
        /// </summary>
        bool AllowMultiSelection { get; set; }

        /// <summary>
        /// Gets the list of file paths selected by the user.
        /// </summary>
        IReadOnlyCollection<string> FilePaths { get; }
    }
}
