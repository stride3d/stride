// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Updater
{
    /// <summary>
    /// Defines this member should be supported by <see cref="UpdateEngine"/>
    /// even if <see cref="Stride.Core.DataMemberIgnoreAttribute"/> is applied on it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DataMemberUpdatableAttribute : Attribute
    {
    }
}
