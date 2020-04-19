// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Annotations
{
    /// <summary>
    /// This attribute indicates that a collection should not be serialized with identifiers associated to each of its item.
    /// </summary>
    /// <remarks>
    /// When this attribute is attached to a type (class or struct), any collection in that type or in a nested type will be serialized without identifier.
    /// When this attribute is attached to a member (property of field) that is a collection, only this collection will be serialized without identifier.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class NonIdentifiableCollectionItemsAttribute : Attribute
    {
    }
}
