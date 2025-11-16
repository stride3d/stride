// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Rendering;

public static partial class ParameterKeys
{
    /// <summary>
    ///   Private structure containing information about a composed parameter key,
    ///   combining a base parameter key with an optional name and indexer.
    /// </summary>
    /// <param name="parameterKey">
    ///   The original <see cref="ParameterKey"/> that this composed key is based on.
    /// </param>
    /// <param name="name">
    ///   The name of the composed parameter key.
    ///   This name will be appended to the name of <paramref name="parameterKey"/>.
    /// </param>
    /// <param name="indexer">
    ///   The optional indexer for the composed parameter key. It must be a non-negative integer.
    ///   If negative, it will be treated as no indexer specified.
    /// </param>
    private readonly struct ParameterComposedKey(ParameterKey parameterKey, string name, int indexer)
        : IEquatable<ParameterComposedKey>
    {
        public const int NoIndexer = -1;

        private readonly ParameterKey key = parameterKey;
        private readonly string name = name;
        private readonly int indexer = indexer >= 0 ? indexer : NoIndexer;

        // Cached hash code for performance optimization
        private readonly int hashCode = HashCode.Combine(parameterKey, name, indexer);

        /// <summary>
        ///   Gets the original parameter key that this composed key is based on.
        /// </summary>
        public ParameterKey Key => key;

        /// <summary>
        ///   Gets the name of the composed parameter key, which is to be appended to the original key.
        /// </summary>
        public string Name => name;

        /// <summary>
        ///   Gets the optional indexer for the composed parameter key.
        /// </summary>
        /// <value>
        ///   The indexer for the composed parameter key, or <see langword="null"/> if no indexer was specified.
        /// </value>
        public int? Indexer => indexer == NoIndexer ? null : indexer;


        /// <inheritdoc/>
        public readonly bool Equals(ParameterComposedKey other)
        {
            return Key.Equals(other.Key)
                && string.Equals(Name, other.Name)
                && Indexer == other.Indexer;
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object obj)
        {
            return obj is ParameterComposedKey parameterComposedKey && Equals(parameterComposedKey);
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode() => hashCode;

        public static bool operator ==(ParameterComposedKey left, ParameterComposedKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ParameterComposedKey left, ParameterComposedKey right)
        {
            return !left.Equals(right);
        }
    }
}
