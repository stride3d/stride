// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Storage;

/// <summary>
/// Description of a bundle: header, dependencies, objects and assets.
/// </summary>
public class BundleDescription
{
    public BundleOdbBackend.Header Header { get; set; }

    public List<string> Dependencies { get; }
    public List<ObjectId> IncrementalBundles { get; }
    public List<KeyValuePair<ObjectId, BundleOdbBackend.ObjectInfo>> Objects { get; }
    public List<KeyValuePair<string, ObjectId>> Assets { get; }

    public BundleDescription()
    {
        Dependencies = [];
        IncrementalBundles = [];
        Objects = [];
        Assets = [];
    }
}
