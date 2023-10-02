using System.Collections;
using System.Collections.Generic;
using Stride.Core.Collections;

namespace Stride.Core.Settings
{
    /// <summary>
    /// Collection of runtime loaded application settings. See also <seealso cref="AppSettingsManager"/>.
    /// </summary>
    [DataContract("AppSettings")]
    public sealed class AppSettings : IEnumerable<object>
    {
        private FastCollection<object> settings = new();

        /// <summary>
        /// Application specific settings.
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<object> Settings { get => settings; }

        /// <summary>
        /// Default constructor, used for deserialization.
        /// </summary>
        public AppSettings() { }

        /// <summary>
        /// Creates a new <see cref="AppSettings"/> instance with a settings collection.
        /// </summary>
        /// <param name="settings">Settings collection.</param>
        public AppSettings(IEnumerable<object> settings) => this.settings = new FastCollection<object>(settings);

        /// <summary>
        /// Finds a settings object of the specified type in the settings collection.
        /// </summary>
        /// <returns>Found object, or null if not found.</returns>
        public T GetSettings<T>() where T : class
        {
            if (Settings == null)
                return null;

            foreach (var obj in Settings)
                if (obj is T setting)
                    return setting;

            return null;
        }

        /// <summary>
        /// Inline Enumerator used by foreach.
        /// </summary>
        /// <returns>Enumerator of the underlying settings collection.</returns>
        public FastCollection<object>.Enumerator GetEnumerator() => settings.GetEnumerator();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => ((IEnumerable<object>)settings).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)settings).GetEnumerator();
    }
}
