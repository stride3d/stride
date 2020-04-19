// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core
{
    /// <summary>
    /// When specified on a property or field, it will not be used when serializing/deserializing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public class DataMemberIgnoreAttribute : Attribute
    {
    }
}
