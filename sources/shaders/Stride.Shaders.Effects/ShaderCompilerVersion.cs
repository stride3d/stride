// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders;

/// <summary>
///   Versions of the Shader compiler output, used to invalidate the Shader caches.
/// </summary>
/// <remarks>
///   <para>
///     Shader caches are keyed on Shader inputs and never on compiler code, so a compiler change that
///     alters the output does not invalidate them by itself: users would silently keep the old output
///     and the change would look like it did nothing. Bumping one of these versions is what does it.
///   </para>
///   <para>
///     There are two caches, one derived from the other. <c>FileShaderCache</c> holds the SPIR-V compiled
///     from each Shader class, and is stamped with <see cref="Shader"/>. The Effect caches
///     (<see cref="ShaderMixinObjectId"/> at runtime, <c>EffectCompileCommand</c> in the asset build) hold
///     what is derived from that SPIR-V, and are keyed on both <see cref="Shader"/> and <see cref="Effect"/>.
///     So a <see cref="Shader"/> bump invalidates everything, while an <see cref="Effect"/> bump leaves the
///     SPIR-V cache alone.
///   </para>
///   <para>
///     This is not <see cref="EffectBytecode.MagicHeader"/>, which only changes when the serialized
///     layout of <see cref="EffectBytecode"/> itself changes.
///   </para>
/// </remarks>
public static class ShaderCompilerVersion
{
    /// <summary>
    ///   Bump when the SPIR-V compiled from a Shader changes: a different constant buffer layout, resource
    ///   order, or code generation. Invalidates the Shader cache and the Effect caches.
    /// </summary>
    public const int Shader = 2;

    /// <summary>
    ///   Bump when what is derived from the SPIR-V changes while the SPIR-V does not: reflection, or the
    ///   bytecode compiled from it for each graphics API. Invalidates the Effect caches only.
    /// </summary>
    public const int Effect = 0;
}
