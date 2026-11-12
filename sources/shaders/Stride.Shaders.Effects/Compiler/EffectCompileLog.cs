// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Compiler;

/// <summary>
///   The log of an effect compile. Its messages carry its module, also those that other loggers created without one,
///   such as the shader loader, so that a log listener can filter them by module.
/// </summary>
public class EffectCompileLog : LoggerResult
{
    /// <summary>
    ///   The module of the messages of the effect compiler: SDSL errors, warnings and notices, and the shader compilers
    ///   that run after it, except the messages that have a module of their own (see <see cref="ShaderBackendLog"/>).
    /// </summary>
    public const string EffectCompilerModule = "EffectCompiler";

    /// <summary>
    ///   The module of the messages of the effect compiler cache, such as the effects it compiled.
    /// </summary>
    public const string EffectCompilerCacheModule = "EffectCompilerCache";

    /// <summary>
    ///   Initializes a new instance of the <see cref="EffectCompileLog"/> class.
    /// </summary>
    /// <param name="module">The module the messages get when they come without one.</param>
    public EffectCompileLog(string module)
        : base(module)
    {
    }

    /// <inheritdoc/>
    protected override void LogRaw(ILogMessage logMessage)
    {
        logMessage.Module ??= Module;
        base.LogRaw(logMessage);
    }
}
