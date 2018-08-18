// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_ANDROID
using Android.Widget;
using OpenTK.Platform.Android;
using Xenko.Games.Android;

namespace Xenko.Games
{
    /// <summary>
    /// A <see cref="GameContext"/> to use for rendering to an existing WinForm <see cref="Control"/>.
    /// </summary>
    public partial class GameContextAndroid : GameContext<AndroidXenkoGameView>
    {
        /// <inheritDoc/>
        public GameContextAndroid(AndroidXenkoGameView control, RelativeLayout editTextLayout, int requestedWidth = 0, int requestedHeight = 0)
            : base(control, requestedWidth, requestedHeight)
        {
            EditTextLayout = editTextLayout;
            ContextType = AppContextType.Android;
        }

        /// <summary>
        /// The layout used to add the <see cref="EditText"/>s.
        /// </summary>
        public readonly RelativeLayout EditTextLayout;
    }
}
#endif
