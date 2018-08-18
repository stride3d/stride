// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Games
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
#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_UNIX
    #if XENKO_GRAPHICS_API_OPENGL
        #if XENKO_UI_SDL
            type = AppContextType.DesktopSDL;
        #elif XENKO_UI_OPENTK
            type = AppContextType.DesktopOpenTK;
        #endif
    #elif XENKO_GRAPHICS_API_VULKAN
        #if XENKO_UI_SDL && !XENKO_UI_WINFORMS && !XENKO_UI_WPF
            type = AppContextType.DesktopSDL;
        #endif
    #else
            type = AppContextType.Desktop;
    #endif
#elif XENKO_PLATFORM_UWP
            type = AppContextType.UWPXaml; // Can change later to CoreWindow
#elif XENKO_PLATFORM_ANDROID
            type = AppContextType.Android;
#elif XENKO_PLATFORM_IOS
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
#if XENKO_PLATFORM_IOS
            return new GameContextiOS(new iOSWindow(null, null, null), 0, 0);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextAndroid()
        {
#if XENKO_PLATFORM_ANDROID
            return new GameContextAndroid(null, null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextDesktop()
        {
#if XENKO_PLATFORM_WINDOWS_DESKTOP
    #if XENKO_UI_OPENTK
            return new GameContextOpenTK(null);
    #else
        #if XENKO_UI_SDL && !XENKO_UI_WINFORMS && !XENKO_UI_WPF
            return new GameContextSDL(null);
        #elif (XENKO_UI_WINFORMS || XENKO_UI_WPF)
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
#if XENKO_PLATFORM_UWP
            return new GameContextUWPXaml(null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextUWPCoreWindow()
        {
#if XENKO_PLATFORM_UWP
            return new GameContextUWPCoreWindow(null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextOpenTK()
        {
#if (XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_UNIX) && XENKO_GRAPHICS_API_OPENGL && XENKO_UI_OPENTK
            return new GameContextOpenTK(null);
#else
            return null;
#endif
        }

        public static GameContext NewGameContextSDL()
        {
#if XENKO_UI_SDL
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
