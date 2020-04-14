// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.ViewModel.Logs
{
    public struct LogKey : IEquatable<LogKey>
    {
        public readonly AssetId AssetId;
        public readonly string Name;

        private LogKey(AssetId assetId, [NotNull] string name)
        {
            AssetId = assetId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static LogKey Get([NotNull] string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return new LogKey(AssetId.Empty, name);
        }

        public static LogKey Get(AssetId assetId, [NotNull] string name)
        {
            if (assetId == AssetId.Empty) throw new ArgumentException(@"The guid cannot be null.", nameof(assetId));
            if (name == null) throw new ArgumentNullException(nameof(name));
            return new LogKey(assetId, name);
        }

        public static bool operator ==(LogKey left, LogKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LogKey left, LogKey right)
        {
            return !left.Equals(right);
        }

        public bool Equals(LogKey other)
        {
            return AssetId.Equals(other.AssetId) && string.Equals(Name, other.Name);
        }

        public override string ToString()
        {
            return AssetId + Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LogKey && Equals((LogKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AssetId.GetHashCode() * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
}
