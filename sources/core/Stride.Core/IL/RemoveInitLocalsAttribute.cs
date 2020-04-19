// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.IL
{
    /// <summary>
    /// Using this optimization attribute will prevent local variables in this method to be zero-ed in the prologue (if the runtime supports it).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RemoveInitLocalsAttribute : Attribute
    {
    }
}
