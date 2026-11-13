// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Compilers;

/// <summary>
/// A shader failed to compile, and its errors are already in the log of the compilation that compiled it.
/// </summary>
internal sealed class ShaderErrorsLoggedException(ILogger log, IReadOnlyList<ILogMessage> errors) : InvalidOperationException(Format(errors))
{
    public static string Format(IEnumerable<ILogMessage> errors) => string.Join(Environment.NewLine, errors.Select(error => error.ToString()));

    /// <summary>
    /// Adds the errors to <paramref name="target"/>, unless they are already there: a compilation that waited for this shader gets the same exception.
    /// </summary>
    public void CopyTo(ILogger target)
    {
        if (target == log)
            return;

        foreach (var error in errors)
            target.Log(error);
    }
}
