// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// This attribute can be either used on class or interfaces to scan for types inheriting from them, or on an attribute to scan for types having this specific attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class AssemblyScanAttribute : Attribute
    {
    }
}
