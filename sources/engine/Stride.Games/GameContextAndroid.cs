// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using Android.Widget;
using Stride.Starter;

namespace Stride.Games
{
    /// <summary>
    /// A <see cref="GameContext"/> to use for rendering on Android.
    /// </summary>
    public partial class GameContextAndroid : GameContextSDL
    {
        private readonly StrideActivity strideActivity;
        private PopupWindow editTextPopupWindow;

        internal bool RecreateEditTextPopupWindow { get; set; } = true;

        /// <inheritDoc/>
        public GameContextAndroid(Stride.Graphics.SDL.Window control, StrideActivity strideActivity)
            : base(control)
        {
            this.strideActivity = strideActivity;
            ContextType = AppContextType.Android;
        }

        internal PopupWindow CreateEditTextPopup(EditText editText)
        {
            editTextPopupWindow = strideActivity.CreateEditTextPopup(editText);
            return editTextPopupWindow;
        }

        internal void ShowEditTextPopup() => strideActivity.ShowEditTextPopup(editTextPopupWindow);

        internal void HideEditTextPopup() => strideActivity.HideEditTextPopup(editTextPopupWindow);

        /// <summary>
        /// Shows a message that ends the game, and waits until the player closes it.
        /// </summary>
        /// <returns><see langword="false"/> when there is no activity to show it.</returns>
        internal bool ShowFatalMessage(string title, string message)
        {
            if (strideActivity is null)
                return false;

            strideActivity.ShowFatalMessage(title, message);
            return true;
        }
    }
}
#endif
