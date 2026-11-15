// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

#nullable enable

namespace Stride.Rendering;

/// <summary>
/// Marks a type that declares static <see cref="ParameterKey"/> fields by hand.
/// A source generator finds these fields and generates calls to <see cref="ParameterKeys.Merge(ParameterKey, Type, string)"/> to name and register them.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class EffectKeysAttribute : Attribute
{
    /// <summary>
    /// The prefix to use for the generated key names. If null or empty, the type name will be used as the prefix, not including the Keys suffix if present.
    /// For example, if the type is named MyEffectKeys, the generated key names will be prefixed with "MyEffect".
    /// If the type is named MyEffect, the generated key names will be prefixed with "MyEffect".
    /// The final key name will be the prefix followed by the field name with a period separator, e.g. "MyEffect.MyKey".
    /// </summary>
    public string? KeyNamePrefix { get; init; }
}
