// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

namespace Xenko.Core.Presentation.Windows
{
    /// <summary>
    /// Represents a window that can asynchronously close and/or cancel a request to close.
    /// </summary>
    public interface IAsyncClosableWindow
    {
        /// <summary>
        /// Attempts to close the window.
        /// </summary>
        /// <returns>
        /// A task that completes either when the window is closed, or when the request to close has been cancelled.
        /// The result of the task indicates whether the window has been closed.
        /// </returns>
        Task<bool> TryClose();
    }
}