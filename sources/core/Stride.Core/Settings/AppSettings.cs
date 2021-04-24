using System.Collections.Generic;
using Stride.Core.Collections;

namespace Stride.Core.Settings
{
    [DataContract("AppSettings")]
    public sealed class AppSettings
    {
        /// <summary>
        /// Application specific settings.
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<object> Settings { get; private set; }

        /// <summary>
        /// Default constructor, used for deserialization.
        /// </summary>
        public AppSettings() { }

        /// <summary>
        /// Creates a new <see cref="AppSettings"/> instance with a settings collection.
        /// </summary>
        /// <param name="settings">Settings collection.</param>
        public AppSettings(IEnumerable<object> settings) => Settings = new FastList<object>(settings);

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
    }
}
