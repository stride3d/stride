// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Parsing;

/// <summary>
/// Marks the error of an old spelling that the parser reads past as its new form: in its upgrade mode (<see cref="SDSLParser.Parse"/>),
/// such an error is a note, and the shader upgrade reads the class as usual; one of its steps changes the spelling.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class RecoverableInUpgradeAttribute : Attribute;
