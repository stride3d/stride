using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Stride.Core.IO
{
    /// <summary>
    /// Represents a path in a case-insensitive file system.
    /// </summary>
    [Serializable]
    public abstract class UPath : IEquatable<UPath>, ISerializable
    {
        private readonly string _path;

        /// <summary>
        /// Initializes a new instance of the <see cref="UPath" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        protected UPath(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        public string Path => _path;

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj) => Equals(obj as UPath);

        /// <summary>
        /// Determines whether the specified <see cref="UPath" /> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="UPath" /> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="UPath" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(UPath other) => other != null && StringComparer.OrdinalIgnoreCase.Equals(_path, other._path);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, derived from <see cref="StringComparer.OrdinalIgnoreCase" />.</returns>
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(_path);

        /// <summary>
        /// Returns a string that represents this instance.
        /// </summary>
        /// <returns>A string that represents this instance.</returns>
        public override string ToString() => _path;

        /// <summary>
        /// Gets the hash code for the specified <see cref="UPath" />.
        /// </summary>
        /// <param name="obj">The <see cref="UPath" />.</param>
        /// <returns>The hash code for the specified <see cref="UPath" />.</returns>
        public static int GetHashCode(UPath obj) => obj?.GetHashCode() ?? 0;

        /// <summary>
        /// Determines whether the specified <see cref="UPath" /> instances are equal.
        /// </summary>
        /// <param name="path1">The first <see cref="UPath" /> to compare.</param>
        /// <param name="path2">The second <see cref="UPath" /> to compare.</param>
        /// <returns><c>true</c> if the specified <see cref="UPath" /> instances are equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(UPath path1, UPath path2) => path1?.Equals(path2) ?? path2 == null;

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected UPath(SerializationInfo info, StreamingContext context)
        {
            _path = info.GetString(nameof(_path));
        }

        /// <summary>
        /// Populates a <see cref="SerializationInfo" /> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_path), _path);
        }
    }
}
