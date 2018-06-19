// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Services;

namespace Xenko.GameStudio.Services
{
    /// <summary>
    /// An interface representing a modal dialog to prompt user some credentials
    /// to access a remote host.
    /// </summary>
    public interface ICredentialsDialog : IModalDialog
    {
        /// <summary>
        /// Are credentials obtained valid after closing the modal dialog?
        /// </summary>
        bool AreCredentialsValid { get; }
    }

}
