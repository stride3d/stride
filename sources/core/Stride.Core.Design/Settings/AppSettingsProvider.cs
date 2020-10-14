using System.IO;
using System.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Settings
{
    /// <summary>
    /// Implementation of <see cref="IAppSettingsProvider"/> which uses YAML deserializer to read the settings file.
    /// </summary>
    internal class AppSettingsProvider : IAppSettingsProvider
    {
        const string SettingsExtension = ".appsettings";

        /// <inheritdoc/>
        public AppSettings LoadAppSettings()
        {
            var execFilePath = Assembly.GetEntryAssembly().Location;
            var settingsFilePath = Path.ChangeExtension(execFilePath, SettingsExtension);
            try
            {
                return YamlSerializer.Load<AppSettings>(settingsFilePath);
            }
            catch (FileNotFoundException)
            {
                return new AppSettings();
            }
        }
    }
}
