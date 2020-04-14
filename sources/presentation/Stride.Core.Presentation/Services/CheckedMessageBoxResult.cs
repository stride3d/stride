// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Presentation.Services
{
    /// <summary>
    /// A structure representing the check status and the button pressed by the user to close a checkable message box.
    /// </summary>
    public struct CheckedMessageBoxResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckedMessageBoxResult"/> structure.
        /// </summary>
        /// <param name="messageBoxResult">The result of the message box.</param>
        /// <param name="isChecked">The check status of the message box.</param>
        public CheckedMessageBoxResult(MessageBoxResult messageBoxResult, bool? isChecked)
        {
            MessageBoxResult = messageBoxResult;
            Result = (int)MessageBoxResult;
            IsChecked = isChecked;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckedMessageBoxResult"/> structure.
        /// </summary>
        /// <param name="result">The result of the message box.</param>
        /// <param name="isChecked">The check status of the message box.</param>
        public CheckedMessageBoxResult(int result, bool? isChecked)
        {
            MessageBoxResult = MessageBoxResult.None;
            Result = result;
            IsChecked = isChecked;
        }

        /// <summary>
        /// Gets the result of the message box.
        /// </summary>
        public MessageBoxResult MessageBoxResult { get; }

        /// <summary>
        /// Gets the result of the message box.
        /// </summary>
        public int Result { get; }

        /// <summary>
        /// Gets the check status of the message box.
        /// </summary>
        public bool? IsChecked { get; }
    }
}
