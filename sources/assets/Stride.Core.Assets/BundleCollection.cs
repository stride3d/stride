// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

/// <summary>
/// A collection of bundles.
/// </summary>
[DataContract("!Bundles")]
public class BundleCollection : List<Bundle>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BundleCollection"/> class.
    /// </summary>
    /// <param name="package">The package.</param>
    internal BundleCollection(Package package)
    {
        Package = package;
    }

    /// <summary>
    /// Gets the package this bundle collection is defined for.
    /// </summary>
    /// <value>The package.</value>
    [DataMemberIgnore]
    private Package Package { get; }
}
