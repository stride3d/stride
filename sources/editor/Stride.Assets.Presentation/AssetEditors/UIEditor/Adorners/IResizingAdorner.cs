// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Forms;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.UI;

namespace Xenko.Assets.Presentation.AssetEditors.UIEditor.Adorners
{
    internal interface IResizingAdorner
    {
        UIElement GameSideElement { get; }

        ResizingDirection ResizingDirection { get; }

        Cursor GetCursor();

        /// <summary>
        /// Notify the adorner once the resizing operation is completed.
        /// </summary>
        void OnResizingCompleted();

        /// <summary>
        /// Called on the adorner during the resizing operation.
        ///  </summary>
        /// <param name="horizontalChange">The horizontal delta of the resizing.</param>
        /// <param name="verticalChange">The vertical delta of the resizing.</param>
        void OnResizingDelta(float horizontalChange, float verticalChange);
    }
}
