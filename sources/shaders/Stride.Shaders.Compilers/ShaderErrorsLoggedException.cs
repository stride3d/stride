// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Compilers;

/// <summary>
/// A shader failed to compile, and its errors are already in <see cref="Log"/>.
/// </summary>
internal sealed class ShaderErrorsLoggedException(ILogger log, string errors) : InvalidOperationException(errors)
{
    public ILogger Log => log;
}
