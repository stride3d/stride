// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Reflection;

namespace Stride.Core.Serialization.Contents
{
    /// <summary>
    /// Used to detect whether a type is using <see cref="ReferenceSerializer"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [AssemblyScan]
    public class ReferenceSerializerAttribute : Attribute
    {
    }
}
