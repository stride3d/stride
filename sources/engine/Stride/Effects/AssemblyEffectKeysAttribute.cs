// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Rendering;

/// <summary>
///   Indicates that an assembly that has been preprocessed using the Assembly Processor's
///   <c>ParameterKeyProcessor</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class AssemblyEffectKeysAttribute : Attribute;
