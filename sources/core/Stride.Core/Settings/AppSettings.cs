using System.Collections;
using System.Collections.Generic;
using Stride.Core.Collections;

namespace Stride.Core.Settings
{
    [DataContract("AppSettings")]
    public sealed class AppSettings : IEnumerable<object>
    {
        /// <summary>
        /// Application specific settings.
        /// </summary>
        [DataMember]
        private FastCollection<object> Settings { get; set; }

        /// <summary>
        /// Default constructor, used for deserialization.
        /// </summary>
        public AppSettings() { }

        /// <summary>
        /// Creates a new <see cref="AppSettings"/> instance with a settings collection.
        /// </summary>
        /// <param name="settings">Settings collection.</param>
        public AppSettings(IEnumerable<object> settings) => Settings = new FastCollection<object>(settings);

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

        public FastCollection<object>.Enumerator GetEnumerator() => Settings.GetEnumerator();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => ((IEnumerable<object>)Settings).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Settings).GetEnumerator();
    }
}
