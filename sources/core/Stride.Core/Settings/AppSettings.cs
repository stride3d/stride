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
        private FastList<object> Settings { get; set; }

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
