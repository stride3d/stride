// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Storage
{
    /// <summary>
    /// Description of a bundle: header, dependencies, objects and assets.
    /// </summary>
    public class BundleDescription
    {
        public BundleOdbBackend.Header Header { get; set; }

        public List<string> Dependencies { get; private set; }
        public List<ObjectId> IncrementalBundles { get; private set; }
        public List<KeyValuePair<ObjectId, BundleOdbBackend.ObjectInfo>> Objects { get; private set; }
        public List<KeyValuePair<string, ObjectId>> Assets { get; private set; }

        public BundleDescription()
        {
            Dependencies = new List<string>();
            IncrementalBundles = new List<ObjectId>();
            Objects = new List<KeyValuePair<ObjectId, BundleOdbBackend.ObjectInfo>>();
            Assets = new List<KeyValuePair<string, ObjectId>>();
        }
    }
}
