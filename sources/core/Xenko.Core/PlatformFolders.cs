// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Xenko.Core.Annotations;

namespace Xenko.Core
{
    /// <summary>
    /// Folders used for the running platform.
    /// </summary>
    public class PlatformFolders
    {
        // TODO: This class should not try to initialize directories...etc. Try to find another way to do this

        /// <summary>
        /// The system temporary directory.
        /// </summary>
        public static readonly string TemporaryDirectory = GetTemporaryDirectory();

        /// <summary>
        /// The Application temporary directory.
        /// </summary>
        public static readonly string ApplicationTemporaryDirectory = GetApplicationTemporaryDirectory();

        /// <summary>
        /// The application local directory, where user can write local data (included in backup).
        /// </summary>
        public static readonly string ApplicationLocalDirectory = GetApplicationLocalDirectory();

        /// <summary>
        /// The application roaming directory, where user can write roaming data (included in backup).
        /// </summary>
        public static readonly string ApplicationRoamingDirectory = GetApplicationRoamingDirectory();

        /// <summary>
        /// The application cache directory, where user can write data that won't be backup.
        /// </summary>
        public static readonly string ApplicationCacheDirectory = GetApplicationCacheDirectory();

        /// <summary>
        /// The application data directory, where data is deployed.
        /// It could be read-only on some platforms.
        /// </summary>
        public static readonly string ApplicationDataDirectory = GetApplicationDataDirectory();

        /// <summary>
        /// The (optional) application data subdirectory. If not null or empty, /data will be mounted on <see cref="ApplicationDataDirectory"/>/<see cref="ApplicationDataSubDirectory"/>
        /// </summary>
        /// <remarks>This property should not be written after the VirtualFileSystem static initialization. If so, an InvalidOperationExeception will be thrown.</remarks>
        public static string ApplicationDataSubDirectory
        {
            get { return applicationDataSubDirectory; }

            set
            {
                if (virtualFileSystemInitialized) 
                    throw new InvalidOperationException("ApplicationDataSubDirectory cannot be modified after the VirtualFileSystem has been initialized."); 
                
                applicationDataSubDirectory = value;
            }
        }

        /// <summary>
        /// The application directory, where assemblies are deployed.
        /// It could be read-only on some platforms.
        /// </summary>
        public static readonly string ApplicationBinaryDirectory = GetApplicationBinaryDirectory();

        /// <summary>
        /// Get the path to the application executable.
        /// </summary>
        /// <remarks>Might be null if start executable is unknown.</remarks>
        public static readonly string ApplicationExecutablePath = GetApplicationExecutablePath();

        private static string applicationDataSubDirectory = string.Empty;

        private static bool virtualFileSystemInitialized;

        public static bool IsVirtualFileSystemInitialized
        {
            get
            {
                return virtualFileSystemInitialized;
            }
            internal set
            {
                virtualFileSystemInitialized = value;
            }
        }

        [NotNull]
        private static string GetApplicationLocalDirectory()
        {
#if XENKO_PLATFORM_ANDROID
            var directory = Path.Combine(PlatformAndroid.Context.FilesDir.AbsolutePath, "local");
            Directory.CreateDirectory(directory);
            return directory;
#elif XENKO_PLATFORM_UWP
            return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#elif XENKO_PLATFORM_IOS
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", "Local");
            Directory.CreateDirectory(directory);
            return directory;
#else
            // TODO: Should we add "local" ?
            var directory = Path.Combine(GetApplicationBinaryDirectory(), "local");
            Directory.CreateDirectory(directory);
            return directory;
#endif
        }

        [NotNull]
        private static string GetApplicationRoamingDirectory()
        {
#if XENKO_PLATFORM_ANDROID
            var directory = Path.Combine(PlatformAndroid.Context.FilesDir.AbsolutePath, "roaming");
            Directory.CreateDirectory(directory);
            return directory;
#elif XENKO_PLATFORM_UWP
            return Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
#elif XENKO_PLATFORM_IOS
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", "Roaming");
            Directory.CreateDirectory(directory);
            return directory;
#else
            // TODO: Should we add "local" ?
            var directory = Path.Combine(GetApplicationBinaryDirectory(), "roaming");
            Directory.CreateDirectory(directory);
            return directory;
#endif
        }

        [NotNull]
        private static string GetApplicationCacheDirectory()
        {
#if XENKO_PLATFORM_ANDROID
            var directory = Path.Combine(PlatformAndroid.Context.FilesDir.AbsolutePath, "cache");
#elif XENKO_PLATFORM_UWP
            var directory = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "cache");
#elif XENKO_PLATFORM_IOS
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", "Caches");
#else
            // TODO: Should we add "local" ?
            var directory = Path.Combine(GetApplicationBinaryDirectory(), "cache");
#endif
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static string GetApplicationExecutablePath()
        {
#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_MONO_MOBILE || XENKO_PLATFORM_UNIX
            return Assembly.GetEntryAssembly()?.Location;
#else
            return null;
#endif
        }

        [NotNull]
        private static string GetTemporaryDirectory()
        {
            return GetApplicationTemporaryDirectory();
        }

        [NotNull]
        private static string GetApplicationTemporaryDirectory()
        {
#if XENKO_PLATFORM_ANDROID
            return PlatformAndroid.Context.CacheDir.AbsolutePath;
#elif XENKO_PLATFORM_UWP
            return Windows.Storage.ApplicationData.Current.TemporaryFolder.Path;
#elif XENKO_PLATFORM_IOS
            return Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "tmp");
#else
            return Path.GetTempPath();
#endif
        }

        [NotNull]
        private static string GetApplicationBinaryDirectory()
        {
#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_MONO_MOBILE || XENKO_PLATFORM_UNIX
            var executableName = GetApplicationExecutablePath();
            if (!string.IsNullOrEmpty(executableName))
            {
                return Path.GetDirectoryName(executableName);
            }
    #if XENKO_RUNTIME_CORECLR
            return AppContext.BaseDirectory;
    #else
            return AppDomain.CurrentDomain.BaseDirectory;
    #endif
#elif XENKO_PLATFORM_UWP
            return Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
#else
            throw new NotImplementedException();
#endif
        }

        [NotNull]
        private static string GetApplicationDataDirectory()
        {
#if XENKO_PLATFORM_ANDROID
            return Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data/" + PlatformAndroid.Context.PackageName + "/data";
#elif XENKO_PLATFORM_IOS
            return Foundation.NSBundle.MainBundle.BundlePath + "/data";
#elif XENKO_PLATFORM_UWP
            return Windows.ApplicationModel.Package.Current.InstalledLocation.Path + @"\data";
#else
            return Path.Combine(GetApplicationBinaryDirectory(), "data");
#endif
        }
    }
}
