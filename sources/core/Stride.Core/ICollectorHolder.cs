// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core;

/// <summary>
///   Interface for objects that can collect other object instances that can be released / disposed in bulk.
/// </summary>
public interface ICollectorHolder
{
    /// <summary>
    ///   Gets the collector of associated objects that can be released / disposed.
    /// </summary>
    ObjectCollector Collector { get; }
}
