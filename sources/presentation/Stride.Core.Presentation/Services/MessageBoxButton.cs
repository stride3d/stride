// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Presentation.Services
{
    // TODO: make these enum independent from their System.Windows equivalent
    /// <summary>
    /// An enum representing the buttons to display in a message box.
    /// </summary>
    public enum MessageBoxButton
    {
        /// <summary>
        /// Display a single OK button.
        /// </summary>
        OK = 0,
        /// <summary>
        /// Display a OK button and a Cancel button.
        /// </summary>
        OKCancel = 1,
        /// <summary>
        /// Display a Yes button, a No button and a Cancel button.
        /// </summary>
        YesNoCancel = 3,
        /// <summary>
        /// Display a Yes button and a No button.
        /// </summary>
        YesNo = 4,
    }
}
