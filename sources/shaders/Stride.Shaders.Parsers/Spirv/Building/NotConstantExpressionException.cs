// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Spirv.Building;

/// <summary>
/// Thrown when an expression compiled as a constant (see <see cref="ExpressionExtensions.CompileConstantValue"/>) is not one.
/// </summary>
public class NotConstantExpressionException(string message) : InvalidOperationException(message);
