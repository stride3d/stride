// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Games
{
    /// <summary>
    /// Given a <see cref="AppContextType"/> creates the corresponding GameContext instance based on the current executing platform.
    /// </summary>
    public static class GameContextFactory
    {
        [Obsolete("Use NewGameContext with the proper AppContextType.")]
        internal static GameContext NewDefaultGameContext()
        {
            // Default context is Desktop
            AppContextType type = AppContextType.Desktop;
#if STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UNIX
    #if STRIDE_GRAPHICS_API_OPENGL
        #if STRIDE_UI_SDL
            type = AppContextType.DesktopSDL;
        #elif STRIDE_UI_OPENTK
            type = AppContextType.DesktopOpenTK;
        #endif
    #elif STRIDE_GRAPHICS_API_VULKAN
        #if STRIDE_UI_SDL && !STRIDE_UI_WINFORMS && !STRIDE_UI_WPF
            type = AppContextType.DesktopSDL;
        #endif
    #else
            type = AppContextType.Desktop;
    #endif
#elif STRIDE_PLATFORM_UWP
            type = AppContextType.UWPXaml; // Can change later to CoreWindow
#elif STRIDE_PLATFORM_ANDROID
            type = AppContextType.Android;
#elif STRIDE_PLATFORM_IOS
            type = AppContextType.iOS;
#endif
            return NewGameContext(type);
        }

        /// <summary>
        /// Given a <paramref name="type"/> create the appropriate game Context for the current executing platform.
        /// </summary>
        /// <returns></returns>
        public static GameContext NewGameContext(AppContextType type)
        {
            GameContext res = null;
            switch (type)
            {
                case AppContextType.Android:
                    res = NewGameContextAndroid();
                    break;
                case AppContextType.Desktop:
                    res = NewGameContextDesktop();
                    break;
                case AppContextType.DesktopOpenTK:
                    res = NewGameContextOpenTK();
                    break;
                case AppContextType.DesktopSDL:
                    res = NewGameContextSDL();
                    break;
                case AppContextType.DesktopWpf:
                    res = NewGameContextWpf();
                    break;
                case AppContextType.UWPXaml:
                    res = NewGameContextUWPXaml();
                    break;
                case AppContextType.UWPCoreWindow:
                    res = NewGameContextUWPCoreWindow();
                    break;
                case AppContextType.iOS:
                    res = NewGameContextiOS();
                    break;
            }

            if (res == null)
            {
                throw new InvalidOperationException("Requested type and current platform are incompatible.");
            }

            return res;
        }

        public static GameContext NewGameContextiOS()
        {
#if STRIDE_PLATFORM_IOS
            return new GameContextiOS(new iOSWindow(null, null, null), 0, 0);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextAndroid()
        {
#if STRIDE_PLATFORM_ANDROID
            return new GameContextAndroid(null, null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextDesktop()
        {
#if STRIDE_PLATFORM_WINDOWS_DESKTOP
    #if STRIDE_UI_OPENTK
            return new GameContextOpenTK(null);
    #else
        #if STRIDE_UI_SDL && !STRIDE_UI_WINFORMS && !STRIDE_UI_WPF
            return new GameContextSDL(null);
        #elif (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
            return new GameContextWinforms(null);
        #else
            return null;
        #endif
    #endif
#else
            return null;
#endif
        }

        public static GameContext NewGameContextUWPXaml()
        {
#if STRIDE_PLATFORM_UWP
            return new GameContextUWPXaml(null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextUWPCoreWindow()
        {
#if STRIDE_PLATFORM_UWP
            return new GameContextUWPCoreWindow(null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextOpenTK()
        {
#if (STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UNIX) && STRIDE_GRAPHICS_API_OPENGL && STRIDE_UI_OPENTK
            return new GameContextOpenTK(null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextSDL()
        {
#if STRIDE_UI_SDL
            return new GameContextSDL(null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextWpf()
        {
            // Not supported for now.
            return null;
        }
    }
}
