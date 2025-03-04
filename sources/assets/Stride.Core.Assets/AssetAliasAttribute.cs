// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

/// <summary>
/// Associates a type name used in YAML content.
/// </summary>
public class AssetAliasAttribute : Attribute
{

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetAliasAttribute"/> class.
    /// </summary>
    /// <param name="alias">The type name.</param>
    public AssetAliasAttribute(string @alias)
    {
        Alias = alias;
    }

    /// <summary>
    /// Gets the type name.
    /// </summary>
    /// <value>The type name.</value>
    public string Alias { get; }
}
