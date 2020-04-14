// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Engine
{
    /// <summary>
    /// Allows a component of the same type to be added multiple time to the same entity (default is <c>false</c>)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AllowMultipleComponentsAttribute : EntityComponentAttributeBase
    {
    }
}
