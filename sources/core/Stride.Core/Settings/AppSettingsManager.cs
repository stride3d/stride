using System;
using Stride.Core.Reflection;

namespace Stride.Core.Settings
{
    /// <summary>
    /// Manages the loading of application settings with <see cref="IAppSettingsProvider"/>.
    /// </summary>
    public static class AppSettingsManager
    {
        private static AppSettings settings;
        private static IAppSettingsProvider provider;

        /// <summary>
        /// Gets <see cref="AppSettings"/> instance for the application.
        /// </summary>
        /// <remarks>
        /// Loaded with an <see cref="IAppSettingsProvider"/>. If no provider implementation is found, returns empty <see cref="AppSettings"/> instance.
        /// </remarks>
        public static AppSettings Settings
        {
            get
            {
                if (settings != null)
                    return settings;

                ReloadSettings();
                return settings;
            }
        }

        /// <summary>
        /// Gets or sets an <see cref="IAppSettingsProvider"/> for the application.
        /// </summary>
        /// <remarks>
        /// If provider is not set, getter of this property will attempt to find an implementation
        /// among the registered assemblies and cache it.
        /// </remarks>
        public static IAppSettingsProvider SettingsProvider
        {
            get
            {
                if (provider != null)
                    return provider;

                foreach (var assembly in AssemblyRegistry.FindAll())
                {
                    var scanTypes = AssemblyRegistry.GetScanTypes(assembly);
                    if (scanTypes != null &&
                        scanTypes.Types.TryGetValue(typeof(IAppSettingsProvider), out var providerTypes))
                    {
                        foreach (var type in providerTypes)
                            if (!type.IsAbstract &&
                                type.GetConstructor(Type.EmptyTypes) != null)
                            {
                                provider = (IAppSettingsProvider)Activator.CreateInstance(type);
                                return provider;
                            }
                    }
                }

                return null;
            }
            set
            {
                provider = value;
            }
        }

        /// <summary>
        /// Clears cached settings value and calls <see cref="IAppSettingsProvider.LoadAppSettings"/> on the <see cref="SettingsProvider"/>.
        /// </summary>
        public static void ReloadSettings()
        {
            if (SettingsProvider != null)
            {
                settings = SettingsProvider.LoadAppSettings();
            }
            else
            {
                settings = new AppSettings();
            }
        }
    }
}
