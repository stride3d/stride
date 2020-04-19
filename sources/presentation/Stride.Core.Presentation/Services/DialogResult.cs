// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Presentation.Services
{
    // TODO: make these enum independent from their System.Windows equivalent
    /// <summary>
    /// An enum representing the result of a dialog invocation.
    /// </summary>
    public enum DialogResult
    {
        /// <summary>
        /// The dialog has not been invoked or closed yet.
        /// </summary>
        None = 0,
        /// <summary>
        /// The dialog has been closed by a validation from the user.
        /// </summary>
        Ok = 1,
        /// <summary>
        /// The dialog has been closed by a cancellation from the user.
        /// </summary>
        Cancel = 2,
    };
}
