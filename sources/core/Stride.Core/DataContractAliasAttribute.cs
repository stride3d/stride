// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core;

/// <summary>
/// Gives a type of another library the alias that a <see cref="DataContractAttribute"/> would give it.
/// </summary>
/// <param name="alias">The alias of the type.</param>
/// <param name="type">The type.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class DataContractAliasAttribute(string alias, Type type) : Attribute
{
    /// <summary>
    /// Gets the alias of the type.
    /// </summary>
    public string Alias { get; } = alias;

    /// <summary>
    /// Gets the type.
    /// </summary>
    public Type Type { get; } = type;
}
