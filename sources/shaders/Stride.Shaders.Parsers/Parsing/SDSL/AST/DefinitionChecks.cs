// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL.AST;

/// <summary>
/// Mistakes found when a class compiles that would otherwise show only when some effect mixes it, or never.
/// </summary>
internal static class DefinitionChecks
{
    /// <summary>
    /// An <c>override</c> needs an inherited method with the same name (SDSL0200, warning). One with the same name but another
    /// signature is error SDSL0175 / SDSL0176 (<c>ShaderMethod.CheckOverriddenMethod</c>). It must also be of the same kind, module
    /// or instance, unless the class is root-only (SDSL0206 / SDSL0207).
    /// </summary>
    public static void CheckOverride(SymbolTable table, Identifier name, FunctionType type, bool isModule)
    {
        var inherited = table.InheritedShaders
            .SelectMany(x => x.Symbol!.Methods.Select(m => (Owner: x.Symbol!.Name, m.Symbol, m.Flags)))
            .Where(x => x.Symbol.Id.Name == name.Name)
            .ToList();
        var shaderName = table.CurrentShader!.Name;
        var sameSignature = inherited.Where(x => x.Symbol.Type is FunctionType inheritedType && inheritedType == type).ToList();
        if (inherited.Count == 0)
            table.AddWarning(new(name.Info, string.Format(SDSLErrorMessages.SDSL0200, shaderName, name.Name)));
        else if (sameSignature.Count > 0 && ChecksModuleRules(table))
        {
            // The method this override is chained to: the last one in the linearization
            var parent = sameSignature[^1];
            var parentIsModule = (parent.Flags & FunctionFlagsMask.Stage) != 0;
            if (parentIsModule && !isModule)
                table.AddWarning(new(name.Info, string.Format(SDSLErrorMessages.SDSL0206, shaderName, name.Name, parent.Owner)));
            else if (!parentIsModule && isModule)
                table.AddWarning(new(name.Info, string.Format(SDSLErrorMessages.SDSL0207, shaderName, name.Name, parent.Owner)));
        }
    }

    /// <summary>
    /// <c>base.X()</c> needs an inherited <c>X</c>: the mixer looks for the previous method of the same chain (SDSL0201).
    /// Instance code can't call a module method through <c>base</c> (SDSL0208).
    /// </summary>
    public static void CheckBaseCall(SymbolTable table, MethodCall call)
    {
        if (call.ResolvedFunctionSymbol is not { Type: FunctionType type } || table.CurrentShader == null)
            return;
        var inherited = table.InheritedShaders
            .SelectMany(x => x.Symbol!.Methods)
            .Where(m => m.Symbol.Id.Name == call.Name.Name && m.Symbol.Type is FunctionType inheritedType && inheritedType == type)
            .ToList();
        if (inherited.Count == 0)
            table.AddWarning(new(call.Info, string.Format(SDSLErrorMessages.SDSL0201, table.CurrentShader.Name, call.Name.Name)));
        else if (ChecksModuleRules(table) && table.CurrentMethod is { IsStaged: false } caller && inherited.All(x => (x.Flags & FunctionFlagsMask.Stage) != 0))
            table.AddWarning(new(call.Info, string.Format(SDSLErrorMessages.SDSL0208, table.CurrentShader.Name, call.Name.Name, caller.Name.Name)));
    }

    /// <summary>
    /// The module rules are checked in shaders that are not root-only: module code has one copy per effect, so it can't use
    /// the members of one instance. A module has only module code, and a root-only shader is the effect's only instance.
    /// </summary>
    private static bool ChecksModuleRules(SymbolTable table)
        => table.CurrentShader != null && (table.CurrentShaderFlags & (ShaderFlagsMask.Module | ShaderFlagsMask.Root)) == 0;

    /// <summary>
    /// Module code reading an instance member (SDSL0204). In a <c>[Pure]</c> module, reading <c>streams</c> (SDSL0209).
    /// </summary>
    public static void CheckRead(SymbolTable table, Identifier identifier, Symbol? symbol)
    {
        if (table.CurrentMethod is not { } method)
            return;
        if (identifier.Name == "streams" && table.CurrentShaderFlags.HasFlag(ShaderFlagsMask.Pure))
        {
            table.AddWarning(new(identifier.Info, string.Format(SDSLErrorMessages.SDSL0209, table.CurrentShader!.Name, $"reads streams in {method.Name.Name}()")));
            return;
        }
        if (symbol == null || !method.IsStaged || !ChecksModuleRules(table)
            || symbol.Id.Kind is not (SymbolKind.Variable or SymbolKind.CBuffer or SymbolKind.SamplerState or SymbolKind.TBuffer)
            || symbol.OwnerType is not { } owner
            || !TryGetMemberFlags(owner, symbol, out var flags)
            || (flags & (VariableFlagsMask.Stage | VariableFlagsMask.Stream)) != 0)
            return;
        table.AddWarning(new(identifier.Info, string.Format(SDSLErrorMessages.SDSL0204, table.CurrentShader!.Name, method.Name.Name, identifier.Name, owner.Name)));
    }

    /// <summary>
    /// Module code calling an instance method of its class or a parent (SDSL0205). In a <c>[Pure]</c> module, a call to a pixel
    /// shader intrinsic (SDSL0213).
    /// </summary>
    public static void CheckCall(SymbolTable table, MethodCall call, Symbol? function)
    {
        if (table.CurrentMethod is not { } method)
            return;
        if (function == null)
        {
            if (call.MemberCallBaseType == null ? PixelShaderIntrinsics.Contains(call.Name.Name) : call.MemberCallBaseType is TextureType && AutomaticLevelOfDetailMethods.Contains(call.Name.Name))
                CheckPixelShaderOnly(table, call.Info, $"{call.Name.Name}()");
            return;
        }
        if (function.OwnerType is not { } owner)
            return;
        // Through the class itself: implicit this, `this.` or `base.` (a static reference or a composition has another base type)
        var throughThis = call.MemberCallBaseType is null or ShaderMixinType;
        if (throughThis && method.IsStaged && !function.Id.IsStage && ChecksModuleRules(table))
            table.AddWarning(new(call.Info, string.Format(SDSLErrorMessages.SDSL0205, table.CurrentShader!.Name, method.Name.Name, call.Name.Name, owner.Name)));
    }

    /// <summary>
    /// A <c>[Pure]</c> module uses other modules by name (<c>Math.PI</c>, <c>Global.Time</c>) only if they are pure (SDSL0210).
    /// Reported once per referenced class.
    /// </summary>
    public static void CheckStaticReference(SymbolTable table, TextLocation location, ShaderClassInstantiation reference)
    {
        if (table.CurrentShaderFlags.HasFlag(ShaderFlagsMask.Pure) && !reference.IsPure)
            table.AddWarning(new(location, string.Format(SDSLErrorMessages.SDSL0210, table.CurrentShader!.Name, reference.ClassName)));
    }

    /// <summary>
    /// Names of the intrinsics that need pixel shader derivatives or kill the pixel.
    /// </summary>
    private static readonly HashSet<string> PixelShaderIntrinsics = ["ddx", "ddy", "ddx_coarse", "ddy_coarse", "ddx_fine", "ddy_fine", "fwidth", "clip"];

    /// <summary>
    /// Names of the texture methods that get their level of detail from pixel shader derivatives.
    /// </summary>
    private static readonly HashSet<string> AutomaticLevelOfDetailMethods = ["Sample", "SampleBias", "SampleCmp", "SampleCmpBias", "CalculateLevelOfDetail", "CalculateLevelOfDetailUnclamped"];

    /// <summary>
    /// A <c>[Pure]</c> module runs in any stage: no derivatives, no <c>discard</c> / <c>clip</c> (SDSL0213).
    /// </summary>
    public static void CheckPixelShaderOnly(SymbolTable table, TextLocation location, string what)
    {
        if (table.CurrentShaderFlags.HasFlag(ShaderFlagsMask.Pure) && table.CurrentMethod is { } method)
            table.AddWarning(new(location, string.Format(SDSLErrorMessages.SDSL0213, table.CurrentShader!.Name, what, method.Name.Name)));
    }

    /// <summary>
    /// A <c>[Pure]</c> module declares no state (SDSL0209) and imports only pure modules (SDSL0210).
    /// </summary>
    public static void CheckPureModule(SymbolTable table, ShaderClass shaderClass, List<ShaderClassInstantiation> directParents)
    {
        foreach (var parent in directParents)
        {
            if (parent.IsModule && !parent.IsPure)
                table.AddWarning(new(shaderClass.Name.Info, string.Format(SDSLErrorMessages.SDSL0210, shaderClass.Name.Name, parent.ClassName)));
        }
        foreach (var element in shaderClass.Elements)
        {
            var state = element switch
            {
                ShaderMember { TypeModifier: TypeModifier.Const } => null,
                ShaderMember { StreamKind: not StreamKind.None } member => (member.Info, $"declares the stream {member.Name}"),
                ShaderMember { IsCompose: true } member => (member.Info, $"declares the composition {member.Name}"),
                ShaderMember member => (member.Info, $"declares the variable {member.Name}"),
                ShaderCompose compose => (compose.Info, $"declares the composition {compose.Name}"),
                ShaderBuffer buffer => (buffer.Info, $"declares {buffer.Name}"),
                ShaderSamplerState sampler => (sampler.Info, $"declares the sampler {sampler.Name}"),
                _ => ((TextLocation, string)?)null,
            };
            if (state is var (location, text))
                table.AddWarning(new(location, string.Format(SDSLErrorMessages.SDSL0209, shaderClass.Name.Name, text)));
        }
    }

    /// <summary>
    /// A misspelled attribute does nothing on a class or a cbuffer (SDSL0217). Only a name close to a known one is reported, because libraries read attributes of their own.
    /// </summary>
    public static void CheckAttributes(SymbolTable table, ShaderAttributeList? attributes, string declaration, params string[] known)
    {
        if (attributes == null)
            return;
        foreach (var attribute in attributes.Attributes.OfType<AnyShaderAttribute>())
        {
            if (!known.Contains(attribute.Name.Name) && known.Any(x => IsMisspelling(attribute.Name.Name, x)))
                table.AddWarning(new(attribute.Info, string.Format(SDSLErrorMessages.SDSL0217, attribute.Name.Name, declaration, string.Join(" and ", known.Select(x => $"[{x}]")))));
        }
    }

    /// <summary>
    /// Whether <paramref name="name"/> differs from <paramref name="known"/> by at most one edit per four letters, ignoring case.
    /// </summary>
    private static bool IsMisspelling(string name, string known)
    {
        var limit = known.Length / 4;
        if (Math.Abs(name.Length - known.Length) > limit)
            return false;

        var distances = new int[known.Length + 1];
        for (var j = 0; j <= known.Length; ++j)
            distances[j] = j;
        for (var i = 1; i <= name.Length; ++i)
        {
            var diagonal = distances[0];
            distances[0] = i;
            for (var j = 1; j <= known.Length; ++j)
            {
                var replace = diagonal + (char.ToUpperInvariant(name[i - 1]) == char.ToUpperInvariant(known[j - 1]) ? 0 : 1);
                diagonal = distances[j];
                distances[j] = Math.Min(replace, Math.Min(distances[j], distances[j - 1]) + 1);
            }
        }
        return distances[known.Length] <= limit;
    }

    /// <summary>
    /// The flags of a member variable of <paramref name="owner"/>; false for a local variable or a parameter.
    /// </summary>
    private static bool TryGetMemberFlags(ShaderDefinition owner, Symbol symbol, out VariableFlagsMask flags)
    {
        foreach (var (variable, variableFlags) in owner.Variables)
        {
            // A cbuffer member: the flags are on the cbuffer
            if (variable.Id == symbol.Id && variable.Type == symbol.Type
                || symbol.Id.Kind == SymbolKind.CBuffer && variable.Type is PointerType { BaseType: ConstantBufferSymbol cbuffer } && cbuffer.Members.Any(m => m.Name == symbol.Id.Name))
            {
                flags = variableFlags;
                return true;
            }
        }
        flags = default;
        return false;
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
