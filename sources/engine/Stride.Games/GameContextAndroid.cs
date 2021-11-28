// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using Android.Widget;
using Stride.Graphics.SDL;

namespace Stride.Games
{
    /// <summary>
    /// A <see cref="GameContext"/> to use for rendering on Android.
    /// </summary>
    public partial class GameContextAndroid : GameContextSDL
    {
        /// <inheritDoc/>
        public GameContextAndroid(Window control, RelativeLayout editTextLayout)
            : base(control)
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
