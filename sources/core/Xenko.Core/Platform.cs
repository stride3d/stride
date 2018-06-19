// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Xenko.Core
{
    /// <summary>
    /// Platform specific queries and functions.
    /// </summary>
    public static class Platform
    {
#if XENKO_PLATFORM_WINDOWS_DESKTOP
        /// <summary>
        /// The current running <see cref="PlatformType"/>.
        /// </summary>
        public static readonly PlatformType Type = PlatformType.Windows;
#elif XENKO_PLATFORM_UWP
        /// <summary>
        /// The current running <see cref="PlatformType"/>.
        /// </summary>
        public static readonly PlatformType Type = PlatformType.UWP;
#elif XENKO_PLATFORM_ANDROID
        /// <summary>
        /// The current running <see cref="PlatformType"/>.
        /// </summary>
        public static readonly PlatformType Type = PlatformType.Android;
#elif XENKO_PLATFORM_IOS
        /// <summary>
        /// The current running <see cref="PlatformType"/>.
        /// </summary>
        public static readonly PlatformType Type = PlatformType.iOS;
#elif XENKO_PLATFORM_MACOS
        /// <summary>
        /// The current running <see cref="PlatformType"/>.
        /// </summary>
        public static readonly PlatformType Type = PlatformType.macOS;
#elif XENKO_PLATFORM_LINUX
        /// <summary>
        /// The current running <see cref="PlatformType"/>.
        /// </summary>
        public static readonly PlatformType Type = PlatformType.Linux;
#endif

        /// <summary>
        /// Gets a value indicating whether the running platform is windows desktop.
        /// </summary>
        /// <value><c>true</c> if this instance is windows desktop; otherwise, <c>false</c>.</value>
        public static readonly bool IsWindowsDesktop = Type == PlatformType.Windows;

        /// <summary>
        /// Gets a value indicating whether the running assembly is a debug assembly.
        /// </summary>
        public static readonly bool IsRunningDebugAssembly = GetIsRunningDebugAssembly();

        /// <summary>
        /// Check if running assembly has the DebuggableAttribute set with the `DisableOptimizations` mode enabled.
        /// This function is called only once.
        /// </summary>
        private static bool GetIsRunningDebugAssembly()
        {
#if XENKO_PLATFORM_UWP
            return false;
#else
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var debuggableAttribute = entryAssembly.GetCustomAttributes<DebuggableAttribute>().FirstOrDefault();
                if (debuggableAttribute != null)
                {
#if !XENKO_RUNTIME_CORECLR
                    return (debuggableAttribute.DebuggingFlags & DebuggableAttribute.DebuggingModes.DisableOptimizations) != 0;
#else
                    // Workaround using reflection as CoreCLR does not provide `DebuggingFlags' on DebuggableAttribute. When
                    // using mscorlib from CoreCLR, the field `m_debuggingModes', if it exists, stores this value, so we try
                    // to find it and get its value.
                    try
                    {
                        foreach (var f in debuggableAttribute.GetType().GetTypeInfo().DeclaredFields)
                        {
                            if (f.Name.Equals("m_debuggingModes"))
                            {
                                return ((DebuggableAttribute.DebuggingModes)f.GetValue(debuggableAttribute) & DebuggableAttribute.DebuggingModes.DisableOptimizations) != 0;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Catch all errors 
                    }

                    // Could not find the field holding the `DebuggingFlags', we assume false by default.
                    return false;
#endif
                }
            }
            return false;
#endif
        }
    }
}
