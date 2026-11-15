// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Parsing;

/// <summary>
/// A preprocessor error in a shader. The message has the form file(line,col): error.
/// </summary>
public sealed class ShaderPreprocessorException(string message) : Exception(message);
