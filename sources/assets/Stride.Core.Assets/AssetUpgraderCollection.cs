// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;

namespace Stride.Core.Assets
{
    public class AssetUpgraderCollection
    {
        private struct VersionRange : IComparable<VersionRange>
        {
            private readonly PackageVersion minimum;
            public readonly PackageVersion Target;

            public VersionRange(PackageVersion minimum, PackageVersion target)
            {
                this.minimum = minimum;
                Target = target;
            }

            public bool Contains(PackageVersion value)
            {
                return minimum <= value && value < Target;
            }

            public bool Overlap(VersionRange other)
            {
                return minimum < other.Target && other.minimum < Target;
            }

            public int CompareTo(VersionRange other)
            {
                return minimum.CompareTo(other.minimum);
            }
        }

        private readonly SortedList<VersionRange, Type> upgraders = new SortedList<VersionRange, Type>();
        private readonly Dictionary<Type, IAssetUpgrader> instances = new Dictionary<Type, IAssetUpgrader>();
        private readonly PackageVersion currentVersion;

        public AssetUpgraderCollection(Type assetType, PackageVersion currentVersion)
        {
            this.currentVersion = currentVersion;
            AssetRegistry.IsAssetOrPackageType(assetType, true);
            AssetType = assetType;
        }

        public Type AssetType { get; private set; }

        internal void RegisterUpgrader(Type upgraderType, PackageVersion startVersion, PackageVersion targetVersion)
        {
            lock (upgraders)
            {
                if (targetVersion > currentVersion)
                    throw new ArgumentException("The upgrader has a target version higher that the current version.");

                var range = new VersionRange(startVersion, targetVersion);

                if (upgraders.Any(x => x.Key.Overlap(range)))
                {
                    throw new ArgumentException("The upgrader overlaps with another upgrader.");
                }

                upgraders.Add(new VersionRange(startVersion, targetVersion), upgraderType);
            }
        }

        internal void Validate(PackageVersion minVersion)
        {
            lock (upgraders)
            {
                var version = minVersion;
                foreach (var upgrader in upgraders)
                {
                    if (!upgrader.Key.Contains(version))
                        continue;

                    version = upgrader.Key.Target;
                    if (version == currentVersion)
                        break;
                }

                if (version != currentVersion)
                    throw new InvalidOperationException("No upgrader for asset type [{0}] allow to reach version {1}".ToFormat(AssetType.Name, currentVersion));
            }
        }

        public IAssetUpgrader GetUpgrader(PackageVersion initialVersion, out PackageVersion targetVersion)
        {
            lock (upgraders)
            {
                var upgrader = upgraders.FirstOrDefault(x => x.Key.Contains(initialVersion));
                if (upgrader.Value == null)
                    throw new InvalidOperationException("No upgrader found for version {0} of asset type [{1}]".ToFormat(currentVersion, AssetType.Name));
                targetVersion = upgrader.Key.Target;

                IAssetUpgrader result;
                if (!instances.TryGetValue(upgrader.Value, out result))
                {
                    // Cache the upgrader instances
                    result = (IAssetUpgrader)Activator.CreateInstance(upgrader.Value);
                    instances.Add(upgrader.Value, result);
                }
                return result;
            }
        }
    }
}
