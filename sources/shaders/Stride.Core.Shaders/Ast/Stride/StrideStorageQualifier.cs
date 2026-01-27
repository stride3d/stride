// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Shaders.Ast.Stride;

/// <summary>
///   Defines Stride-specific storage qualifiers.
/// </summary>
public static class StrideStorageQualifier
{
    #region Storage Qualifier keys

    private const string StreamKey = "stream";
    private const string PatchStreamKey = "patchstream";
    private const string StageKey = "stage";
    private const string CloneKey = "clone";
    private const string OverrideKey = "override";
    private const string AbstractKey = "abstract";
    private const string ComposeKey = "compose";
    private const string InternalKey = "internal";

    #endregion

    /// <summary>
    ///   The <c>"stream"</c> modifier.
    ///   Marks a variable as part of the vertex stream.
    ///   Typically used for vertex attributes passed from the CPU to the vertex Shader.
    /// </summary>
    public static readonly Qualifier Stream = new(StreamKey);

    /// <summary>
    ///   The <c>"patchstream"</c> modifier.
    ///   Used in tessellation Shaders. Indicates per-patch data passed between tessellation control
    ///   and evaluation stages.
    /// </summary>
    public static readonly Qualifier PatchStream = new(PatchStreamKey);

    /// <summary>
    ///   The <c>"stage"</c> modifier.
    ///   Marks a variable or function as belonging to a specific Shader stage (e.g., vertex, pixel, compute),
    ///   so it can be accessed from anywhere in the same stage.
    /// </summary>
    public static readonly Qualifier Stage = new(StageKey);

    /// <summary>
    ///   The <c>"clone"</c> modifier.
    ///   Creates a copy of a member or Shader fragment.
    ///   Useful when reusing logic with slight modifications without affecting the original.
    /// </summary>
    public static readonly Qualifier Clone = new(CloneKey);

    /// <summary>
    ///   The <c>"override"</c> modifier.
    ///   Overrides a member from a base Shader class. Used when customizing behavior in derived Shader classes.
    /// </summary>
    public static readonly Qualifier Override = new(OverrideKey);

    /// <summary>
    ///   The <c>"abstract"</c> modifier.
    ///   Declares a member that must be implemented by derived classes.
    /// </summary>
    public static readonly Qualifier Abstract = new(AbstractKey);

    /// <summary>
    ///   The <c>"compose"</c> modifier.
    ///   Combines multiple Shader fragments or mixins. Enables modular shader construction by merging logic
    ///   from different sources.
    /// </summary>
    public static readonly Qualifier Compose = new(ComposeKey);

    /// <summary>
    ///   The <c>"internal"</c> modifier.
    ///   Restricts visibility to the current Shader file or module. Prevents exposure to external Shader compositions.
    /// </summary>
    public static readonly Qualifier Internal = new(InternalKey);


    /// <summary>
    ///   Parses the specified qualifier name into a storage qualifier.
    /// </summary>
    /// <param name="qualifierName">The name of the qualifier to parse.</param>
    /// <returns>A storage <see cref="Qualifier"/>.</returns>
    /// <exception cref="System.ArgumentException">The qualifier name is not recognized.</exception>
    public static Qualifier Parse(string qualifierName)
    {
        return qualifierName switch
        {
            StreamKey => Stream,
            PatchStreamKey => PatchStream,
            StageKey => Stage,
            CloneKey => Clone,
            OverrideKey => Override,
            AbstractKey => Abstract,
            ComposeKey => Compose,
            InternalKey => Internal,

            // Fallback to shared parameter qualifiers
            _ => Hlsl.StorageQualifier.Parse(qualifierName)
        };
    }
}
