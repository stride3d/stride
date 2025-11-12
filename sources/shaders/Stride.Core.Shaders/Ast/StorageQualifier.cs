// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Shaders.Ast;

/// <summary>
///   Defines common Shader storage qualifiers.
/// </summary>
public partial class StorageQualifier
{
    #region Storage Qualifier keys

    private const string ConstKey = "const";
    private const string GroupSharedKey = "groupshared";
    private const string SharedKey = "shared";

    #endregion

    /// <summary>
    ///   The <c>"const"</c> qualifier.
    ///   Indicates that the variable is a compile-time constant. Its value cannot be changed after initialization.
    /// </summary>
    public static readonly Qualifier Const = new(ConstKey);

    /// <summary>
    ///   The <c>"groupshared"</c> modifier.
    ///   Declares a variable that is shared among all threads in a thread group.
    /// </summary>
    public static readonly Qualifier GroupShared = new(GroupSharedKey);

    /// <summary>
    ///   The <c>"shared"</c> modifier.
    ///   Declares a variable that is shared between multiple shader stages or invocations.
    /// </summary>
    public static readonly Qualifier Shared = new(SharedKey);


    /// <summary>
    ///   Parses the specified qualifier name into a storage qualifier.
    /// </summary>
    /// <param name="qualifierName">The name of the qualifier to parse.</param>
    /// <returns>A storage <see cref="Qualifier"/></returns>
    /// <exception cref="ArgumentException">The qualifier name is not recognized.</exception>
    public static Qualifier Parse(string qualifierName)
    {
        return qualifierName switch
        {
            ConstKey => Const,
            GroupSharedKey => GroupShared,
            SharedKey => Shared,

            _ => throw new ArgumentException($"Unable to parse [{qualifierName}] to a qualifier", nameof(qualifierName))
        };
    }
}
