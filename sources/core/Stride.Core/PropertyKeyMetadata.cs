// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core;

/// <summary>
///   Specifies metadata for a <see cref="PropertyKey"/>.
/// </summary>
/// <remarks>
///   This class is used to provide additional information about a property key.
///   Derived classes can implement specific metadata types, such as a default value (<see cref="DefaultValueMetadata"/>),
///   description, or other attributes.
/// </remarks>
public abstract class PropertyKeyMetadata;
