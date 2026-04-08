// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Shaders.Ast.Hlsl;

/// <summary>
///   Defines known HLSL storage qualifiers.
/// </summary>
public static class StorageQualifier
{
    #region Storage Qualifier keys

    private const string ColumnMajorKey = "column_major";
    private const string ExternKey = "extern";
    private const string PreciseKey = "precise";
    private const string RowMajorKey = "row_major";
    private const string StaticKey = "static";
    private const string InlineKey = "inline";
    private const string UnsignedKey = "unsigned";
    private const string VolatileKey = "volatile";

    #endregion

    /// <summary>
    ///   The <c>"column_major"</c> modifier.
    ///   Specifies that matrix data is stored in column-major order in memory.
    /// </summary>
    public static readonly Qualifier ColumnMajor = new(ColumnMajorKey);

    /// <summary>
    ///   The <c>"extern"</c> modifier.
    ///   Indicates that the variable is defined externally and not within the current scope.
    /// </summary>
    public static readonly Qualifier Extern = new(ExternKey);

    /// <summary>
    ///   The <c>"precise"</c> modifier.
    ///   Ensures that calculations involving this variable are performed with maximum precision.
    /// </summary>
    public static readonly Qualifier Precise = new(PreciseKey);

    /// <summary>
    ///   The <c>"row_major"</c> modifier.
    ///   Specifies that matrix data is stored in row-major order in memory.
    /// </summary>
    public static readonly Qualifier RowMajor = new(RowMajorKey);

    /// <summary>
    ///   The <c>"static"</c> modifier.
    ///   Indicates that the variable retains its value between function calls or shader invocations.
    /// </summary>
    public static readonly Qualifier Static = new(StaticKey);

    /// <summary>
    ///   The <c>"inline"</c> modifier.
    ///   Suggests that the function should be inlined, replacing the call with the function body.
    /// </summary>
    public static readonly Qualifier Inline = new(InlineKey);

    /// <summary>
    ///   The <c>"unsigned"</c> modifier.
    ///   Specifies that the variable or type is unsigned.
    /// </summary>
    public static readonly Qualifier Unsigned = new(UnsignedKey);

    /// <summary>
    ///   The <c>"volatile"</c> modifier.
    ///   Indicates that the variable can be modified unexpectedly, such as by another thread or hardware.
    /// </summary>
    public static readonly Qualifier Volatile = new(VolatileKey);


    /// <summary>
    ///   Parses the specified qualifier name into a storage qualifier.
    /// </summary>
    /// <param name="qualifierName">The name of the qualifier to parse.</param>
    /// <returns>A storage <see cref="Qualifier"/>.</returns>
    /// <exception cref="System.ArgumentException">The qualifier name is not recognized.</exception>
    public static Qualifier Parse(string qualifierName)
    {
        Qualifier? qualifier = qualifierName switch
        {
            ColumnMajorKey => ColumnMajor,
            ExternKey => Extern,
            PreciseKey => Precise,
            RowMajorKey => RowMajor,
            StaticKey => Static,
            InlineKey => Inline,
            UnsignedKey => Unsigned,
            VolatileKey => Volatile,

            _ => null
        };

        return qualifier
            // Fallback to parameter interpolation qualifiers
            ?? InterpolationQualifier.Parse(qualifierName)
            // Fallback to GLSL qualifiers
            ?? Glsl.StorageQualifier.Parse(qualifierName)
            // Fallback to common parameter qualifiers
            ?? Ast.StorageQualifier.Parse(qualifierName);
    }
}
