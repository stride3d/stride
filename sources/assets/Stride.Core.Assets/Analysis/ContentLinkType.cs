// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.


namespace Stride.Core.Assets.Analysis;

/// <summary>
/// The different possible types of link between elements.
/// </summary>
[Flags]
public enum ContentLinkType
{
    /// <summary>
    /// A simple reference to the asset.
    /// </summary>
    Reference = 1,

    /// <summary>
    /// The source asset replaces the target (<see cref="Asset.Replaces"/>). A design-time link only:
    /// it is excluded from the default <see cref="Reference"/> queries, so it is never a build dependency.
    /// </summary>
    Replace = 2,

    /// <summary>
    /// All type of links.
    /// </summary>
    All = Reference | Replace,
}
