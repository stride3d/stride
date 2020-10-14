namespace Stride.Core.Settings
{
    /// <summary>
    /// A custom loader of the application settings. Implementation is required to have a parameterless constructor.
    /// </summary>
    /// <note>
    /// We don't want a dependency on complex parsing libraries in Stride.Core,
    /// so the reading of the AppSettings file is left to the implementation of this interface.
    /// </note>
    public interface IAppSettingsProvider
    {
        /// <summary>
        /// Loads <see cref="AppSettings"/> for the application.
        /// </summary>
        AppSettings LoadAppSettings();
    }
}
