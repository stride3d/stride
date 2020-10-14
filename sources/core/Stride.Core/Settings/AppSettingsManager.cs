using System;
using Stride.Core.Reflection;

namespace Stride.Core.Settings
{
    /// <summary>
    /// Manages the loading of application settings with <see cref="IAppSettingsProvider"/>.
    /// </summary>
    public static class AppSettingsManager
    {
        /// <summary>
        /// <see cref="AppSettings"/> instance for the application.
        /// </summary>
        /// <note>
        /// Loaded with an <see cref="IAppSettingsProvider"/>. If no provider implementation is found, returns empty <see cref="AppSettings"/> instance.
        /// </note>
        public static AppSettings Settings
        {
            get
            {
                if (settings != null)
                    return settings;

                foreach (var assembly in AssemblyRegistry.FindAll())
                    foreach (var type in assembly.GetTypes())
                        if (typeof(IAppSettingsProvider).IsAssignableFrom(type))
                        {
                            var ctor = type.GetConstructor(new Type[0]);
                            if (ctor != null)
                            {
                                var instance = (IAppSettingsProvider)ctor.Invoke(new object[0]);
                                settings = instance.LoadAppSettings();
                                return settings;
                            }
                        }

                settings = new AppSettings();
                return settings;
            }
        }

        private static AppSettings settings;
    }
}
