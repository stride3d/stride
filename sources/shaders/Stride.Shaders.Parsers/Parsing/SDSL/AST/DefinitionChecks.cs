// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL.AST;

/// <summary>
/// Mistakes found when a class compiles that would otherwise show only when some effect mixes it, or never.
/// </summary>
internal static class DefinitionChecks
{
    /// <summary>
    /// An <c>override</c> needs an inherited method with the same name (SDSL0200, warning). One with the same name but another
    /// signature is error SDSL0175 / SDSL0176 (<c>ShaderMethod.CheckOverriddenMethod</c>).
    /// </summary>
    public static void CheckOverride(SymbolTable table, Identifier name)
    {
        if (!table.InheritedShaders.Any(x => x.Symbol!.Methods.Any(m => m.Symbol.Id.Name == name.Name)))
            table.AddWarning(new(name.Info, string.Format(SDSLErrorMessages.SDSL0200, table.CurrentShader!.Name, name.Name)));
    }

    /// <summary>
    /// <c>base.X()</c> needs an inherited <c>X</c>: the mixer looks for the previous method of the same chain (SDSL0201).
    /// </summary>
    public static void CheckBaseCall(SymbolTable table, MethodCall call)
    {
        if (call.ResolvedFunctionSymbol is not { Type: FunctionType type } || table.CurrentShader == null)
            return;
        if (!table.InheritedShaders.Any(x => x.Symbol!.Methods.Any(m => m.Symbol.Id.Name == call.Name.Name && m.Symbol.Type is FunctionType inheritedType && inheritedType == type)))
            table.AddWarning(new(call.Info, string.Format(SDSLErrorMessages.SDSL0201, table.CurrentShader.Name, call.Name.Name)));
    }

    /// <summary>
    /// More generic values than parameters for a parent: the extra ones are ignored (SDSL0202). Too few is an error (SDSL0117)
    /// when the parent is loaded.
    /// </summary>
    public static void CheckParentGenericArgumentCount(SymbolTable table, IdentifierBase parent, int given)
    {
        if (!table.ShaderLoader.LoadExternalBuffer(parent.Name, CollectionsMarshal.AsSpan(table.CurrentMacros), out var buffer, out _, out _))
            return;
        var parameters = Spirv.Building.SpirvBuilder.GetGenericParameterNames(buffer.Context);
        if (given > parameters.Count)
            table.AddWarning(new(parent.Info, string.Format(SDSLErrorMessages.SDSL0202, parent.Name, parameters.Count, string.Join(", ", parameters), given)));
    }

    /// <summary>
    /// <c>[Color]</c> works on float3 and float4 values (SDSL0203).
    /// </summary>
    public static void CheckColor(SymbolTable table, TextLocation location, string memberName, SymbolType? type)
    {
        var baseType = type is PointerType pointerType ? pointerType.BaseType : type;
        while (baseType is ArrayType arrayType)
            baseType = arrayType.BaseType;
        if (baseType is not null and not VectorType { BaseType.Type: Scalar.Float, Size: 3 or 4 })
            table.AddWarning(new(location, string.Format(SDSLErrorMessages.SDSL0203, memberName, type is PointerType p ? p.BaseType : type)));
    }
}
