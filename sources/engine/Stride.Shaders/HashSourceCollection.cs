// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Storage;

namespace Stride.Shaders;

[DataContract]
public class HashSourceCollection : Dictionary<string, ObjectId>, IEquatable<HashSourceCollection>
{
    /// <summary>
    /// A dictionary of associations betweens asset shader urls and <see cref="ObjectId"/>
    /// </summary>
    public HashSourceCollection() { }


    public bool Equals(HashSourceCollection other)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HashSourceCollection"/> class.
        /// </summary>
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Utilities.Compare(this, other);
    }

    public override bool Equals(object obj)
    {
        return obj is HashSourceCollection other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Utilities.GetHashCode(this);
    }
}
