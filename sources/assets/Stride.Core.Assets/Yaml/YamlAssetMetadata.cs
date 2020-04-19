// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Yaml
{
    /// <summary>
    /// Equality comparer for <see cref="YamlAssetPath"/> hwne used as a key in a hashing collection (e.g. <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// To stay valid the compared <see cref="YamlAssetPath"/> must not change while used as keys in the hashing collection.
    /// </remarks>
    public class YamlAssetPathComparer : EqualityComparer<YamlAssetPath>
    {
        public new static YamlAssetPathComparer Default { get; } = new YamlAssetPathComparer();

        /// <inheritdoc />
        public override bool Equals(YamlAssetPath x, YamlAssetPath y)
        {
            if (ReferenceEquals(x, y)) return true;
            return x.Match(y);
        }

        /// <inheritdoc />
        public override int GetHashCode(YamlAssetPath obj)
        {
            return obj?.Elements.Aggregate(0, (hashCode, element) => (hashCode * 397) ^ element.GetHashCode()) ?? 0;
        }
    }

    /// <summary>
    /// A container class to transfer metadata between the asset and the YAML serializer.
    /// </summary>
    /// <typeparam name="T">The type of metadata.</typeparam>
    public class YamlAssetMetadata<T> : IYamlAssetMetadata, IEnumerable<KeyValuePair<YamlAssetPath, T>>
    {

        private readonly Dictionary<YamlAssetPath, T> metadata = new Dictionary<YamlAssetPath, T>(YamlAssetPathComparer.Default);
        private bool isAttached;

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="YamlAssetMetadata{T}"/>
        /// </summary>
        public int Count => metadata.Count;

        /// <summary>
        /// Attaches the given metadata value to the given YAML path.
        /// </summary>
        /// <param name="path">The path at which to attach metadata.</param>
        /// <param name="value">The metadata to attach.</param>
        public void Set([NotNull] YamlAssetPath path, T value)
        {
            if (isAttached) throw new InvalidOperationException("Cannot modify a YamlAssetMetadata after it has been attached.");
            metadata[path] = value;
        }

        /// <summary>
        /// Removes attached metadata from the given YAML path.
        /// </summary>
        /// <param name="path">The path at which to remove metadata.</param>
        public void Remove(YamlAssetPath path)
        {
            if (isAttached) throw new InvalidOperationException("Cannot modify a YamlAssetMetadata after it has been attached.");
            metadata.Remove(path);
        }

        /// <summary>
        /// Tries to retrieve the metadata for the given path.
        /// </summary>
        /// <param name="path">The path at which to retrieve metadata.</param>
        /// <returns>The metadata attached to the given path, or the default value of <typeparamref name="T"/> if no metadata is attached at the given path.</returns>
        public T TryGet([NotNull] YamlAssetPath path)
        {
            metadata.TryGetValue(path, out T value);
            return value;
        }

        /// <inheritdoc/>
        void IYamlAssetMetadata.Set(YamlAssetPath path, object value) => Set(path, (T)value);

        /// <inheritdoc/>
        object IYamlAssetMetadata.TryGet(YamlAssetPath path) => TryGet(path);

        /// <inheritdoc/>
        void IYamlAssetMetadata.Attach()
        {
            isAttached = true;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IDictionary)metadata).GetEnumerator();

        public IEnumerator<KeyValuePair<YamlAssetPath, T>> GetEnumerator() => metadata.GetEnumerator();
    }
}
