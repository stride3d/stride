// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Defines the types of a GPU query.
/// </summary>
public enum QueryType
{
    /// <summary>
    ///   Represents a timestamp value, typically used to indicate a point in time.
    ///   Used for measuring the time taken by GPU operations.
    /// </summary>
    Timestamp = 0
}
