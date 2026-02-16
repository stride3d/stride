// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Storage;

namespace Stride.Shaders;

/// <summary>
///   A collection associating the Shader source URLs and their corresponding <see cref="ObjectId"/>s.
/// </summary>
[DataContract]
public sealed class HashSourceCollection : Dictionary<string, ObjectId>, IEquatable<HashSourceCollection>
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="HashSourceCollection"/> class.
    /// </summary>
    public HashSourceCollection() { }


    /// <inheritdoc/>
    public bool Equals(HashSourceCollection other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Utilities.Compare(this, other);
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        return obj is HashSourceCollection other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Utilities.GetHashCode(this);
    }
}
