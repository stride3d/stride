// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core
{
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Class, Inherited = false)]
    public class DataContractIgnoreAttribute : Attribute
    {
    }
}
