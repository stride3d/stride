// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Updater
{
    /// <summary>
    /// Defines this member should be supported by <see cref="UpdateEngine"/>
    /// even if <see cref="Xenko.Core.DataMemberIgnoreAttribute"/> is applied on it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DataMemberUpdatableAttribute : Attribute
    {
    }
}
