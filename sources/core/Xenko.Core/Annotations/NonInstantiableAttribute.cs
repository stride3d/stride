// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.Annotations
{
    /// <summary>
    /// This attribute indicates that the associated type cannot be instanced in the property grid
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class NonInstantiableAttribute : Attribute
    {
    }
}
