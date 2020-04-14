// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Rendering
{
    /// <summary>
    /// Assembly attribute used to mark assembly that has been preprocessed using the <see cref="ParameterKeyProcessor"/>.
    /// Assemblies without this attribute will have all of their type members tagged with <see cref="EffectKeysAttribute"/> scanned for <see cref="ParameterKey"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class AssemblyEffectKeysAttribute : Attribute
    {
    }
}
