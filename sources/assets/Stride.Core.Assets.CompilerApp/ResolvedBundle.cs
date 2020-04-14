// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Storage;

namespace Stride.Core.Assets.CompilerApp
{
    /// <summary>
    /// Helper class that represents additional data added to a <see cref="Bundle"/> when resolving asset.
    /// </summary>
    class ResolvedBundle
    {
        public ResolvedBundle(Bundle source)
        {
            Source = source;
            Name = Source.Name;
        }

        /// <summary>
        /// Name of the bundle.
        /// </summary>
        public string Name;

        /// <summary>
        /// Represented <see cref="Bundle"/>.
        /// </summary>
        public Bundle Source;

        /// <summary>
        /// The asset urls this bundle should include (a.k.a. "root assets").
        /// </summary>
        public HashSet<string> AssetUrls = new HashSet<string>();

        /// <summary>
        /// The bundle dependencies.
        /// </summary>
        public HashSet<ResolvedBundle> Dependencies = new HashSet<ResolvedBundle>();

        /// <summary>
        /// The object ids referenced directly by this bundle.
        /// </summary>
        public HashSet<ObjectId> ObjectIds = new HashSet<ObjectId>();

        /// <summary>
        /// The object ids referenced indirectly by this bundle.
        /// </summary>
        public HashSet<ObjectId> DependencyObjectIds = new HashSet<ObjectId>();

        /// <summary>
        /// The index map that this bundle.
        /// </summary>
        public Dictionary<string, ObjectId> IndexMap = new Dictionary<string, ObjectId>();

        /// <summary>
        /// The index map merged from all this bundle dependencies.
        /// </summary>
        public Dictionary<string, ObjectId> DependencyIndexMap = new Dictionary<string, ObjectId>();

        /// <summary>
        /// The bundle backend.
        /// </summary>
        public BundleOdbBackend BundleBackend;

        public override string ToString()
        {
            return Name;
        }
    }
}
