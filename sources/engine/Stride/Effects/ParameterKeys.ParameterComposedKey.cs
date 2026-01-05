// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Rendering;

public static partial class ParameterKeys
{
    private readonly struct ParameterComposedKey(ParameterKey parameterKey, string name, int indexer)
        : IEquatable<ParameterComposedKey>
    {
        public const int NoIndexer = -1;

        private readonly ParameterKey key = parameterKey;
        private readonly string name = name;
        private readonly int indexer = indexer >= 0 ? indexer : NoIndexer;

        // Cached hash code for performance optimization
        private readonly int hashCode = HashCode.Combine(parameterKey, name, indexer);

        public ParameterKey Key => key;

        public string Name => name;

        public int? Indexer => indexer == NoIndexer ? null : indexer;


        public readonly bool Equals(ParameterComposedKey other)
        {
            return Key.Equals(other.Key)
                && string.Equals(Name, other.Name)
                && Indexer == other.Indexer;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is ParameterComposedKey parameterComposedKey && Equals(parameterComposedKey);
        }

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
