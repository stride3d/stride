// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Diagnostics;

namespace Stride.Shaders.Compiler;

/// <summary>
///   The messages of the compilers that run after SDSL, such as FXC on the HLSL that SPIRV-Cross generates.
///   They are about generated code, so a game shows only their errors; a graphics developer sees the rest with
///   <c>GlobalLogger.SetModuleLevel("ShaderBackend.FXC", LogMessageType.Warning)</c>.
/// </summary>
/// <remarks>
///   The compile result keeps every message, and the filter applies where it is logged: an effect compiled
///   by the effect compiler server follows the level of the game that shows its messages.
/// </remarks>
public static class ShaderBackendLog
{
    /// <summary>
    ///   The module of the messages of FXC (Direct3D 11).
    /// </summary>
    public const string Fxc = "ShaderBackend.FXC";

    private const string ModulePrefix = "ShaderBackend.";

    /// <summary>
    ///   Copies the messages of a compilation, without the back-end messages below the level of their module.
    /// </summary>
    /// <param name="result">The messages of the compilation.</param>
    /// <param name="log">The logger that receives them.</param>
    public static void CopyTo(LoggerResult result, ILogger log)
    {
        foreach (var message in result.Messages)
        {
            if (message.Module is { } module && module.StartsWith(ModulePrefix, StringComparison.Ordinal)
                && !GlobalLogger.GetLogger(module, LogMessageType.Error).Activated(message.Type))
                continue;

            log.Log(message);
        }
    }
}
