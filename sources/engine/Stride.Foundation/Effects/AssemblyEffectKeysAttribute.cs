// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Rendering
{
    /// <summary>
    /// Assembly attribute used to mark assembly that has been preprocessed using the ParameterKeyProcessor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    [Obsolete("The ParameterKeyProcessor has been removed. This attribute will be removed in a future version.")]
    public class AssemblyEffectKeysAttribute : Attribute
    {
    }
}
