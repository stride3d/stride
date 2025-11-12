// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Shaders.Ast.Glsl;

/// <summary>
///   Defines known GLSL storage qualifiers.
/// </summary>
public static class StorageQualifier
{
    #region Storage Qualifier keys

    private const string UniformKey = "uniform";
    private const string BufferKey = "buffer";
    private const string WriteOnlyKey = "writeonly";
    private const string ReadOnlyKey = "readonly";

    #endregion

    /// <summary>
    ///   The <c>"uniform"</c> qualifier.
    ///   Specifies that the variable is a uniform parameter, typically set externally by the application
    ///   and shared across shader invocations.
    /// </summary>
    public static readonly Qualifier Uniform = new(UniformKey);

    /// <summary>
    ///   The <c>"buffer"</c> qualifier.
    ///   Specifies that the variable is associated with a shader storage buffer object (SSBO),
    ///   and can be used for read and write operations.
    /// </summary>
    public static readonly Qualifier Buffer = new(BufferKey);

    /// <summary>
    ///   The <c>"writeonly"</c> qualifier.
    ///   Specifies that the variable is write-only, meaning it can only be written
    ///   to within the shader and not read from.
    /// </summary>
    public static readonly Qualifier WriteOnly = new(WriteOnlyKey);

    /// <summary>
    ///   The <c>"readonly"</c> qualifier.
    ///   Specifies that the variable is read-only, meaning it can only be
    ///   read within the shader and not written to.
    /// </summary>
    public static readonly Qualifier ReadOnly = new(ReadOnlyKey);


    /// <summary>
    ///   Parses the specified qualifier name into a storage qualifier.
    /// </summary>
    /// <param name="qualifierName">The name of the qualifier to parse.</param>
    /// <returns>
    ///   A storage <see cref="Qualifier"/>, or <see langword="null"/> if the qualifier name is not recognized.
    /// </returns>
    public static Qualifier Parse(string qualifierName)
    {
        return qualifierName switch
        {
            UniformKey => Uniform,
            BufferKey => Buffer,
            WriteOnlyKey => WriteOnly,
            ReadOnlyKey => ReadOnly,

            _ => null
        };
    }
}
